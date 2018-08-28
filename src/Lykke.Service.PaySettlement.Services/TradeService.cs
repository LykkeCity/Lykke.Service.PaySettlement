using Autofac;
using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.MatchingEngine.Connector.Models.Api;
using Lykke.MatchingEngine.Connector.Models.Common;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Core.Services;
using Lykke.Service.PaySettlement.Core.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace Lykke.Service.PaySettlement.Services
{
    public class TradeService : IStartable, IStopable, ITradeService
    {
        private readonly ILog _log;
        private readonly IMatchingEngineClient _matchingEngineClient;
        private readonly IPaymentRequestsRepository _paymentRequestsRepository;
        private readonly ITradeOrdersRepository _tradeOrdersRepository;
        private readonly IAssetService _assetPairService;
        private readonly ITransferToMerchantService _transferToMerchantService;
        private readonly ILykkeBalanceService _lykkeBalanceService;
        private readonly IAccuracyRoundHelper _accuracyRoundHelper;                
        private readonly ISettlementStatusPublisher _settlementStatusPublisher;
        private readonly TradeServiceSettings _settings;
        private readonly Timer _timer;

        public TradeService(IMatchingEngineClient matchingEngineClient, 
            IPaymentRequestsRepository paymentRequestsRepository, ITradeOrdersRepository tradeOrdersRepository,
            IAssetService assetPairService, ITransferToMerchantService transferToMerchantService,
            ILykkeBalanceService lykkeBalanceService, ILogFactory logFactory,
            IAccuracyRoundHelper accuracyRoundHelper, ISettlementStatusPublisher settlementStatusPublisher,
            TradeServiceSettings settings)
        {
            _matchingEngineClient = matchingEngineClient;
            _paymentRequestsRepository = paymentRequestsRepository;
            _tradeOrdersRepository = tradeOrdersRepository;
            _assetPairService = assetPairService;
            _transferToMerchantService = transferToMerchantService;
            _lykkeBalanceService = lykkeBalanceService;                        
            _accuracyRoundHelper = accuracyRoundHelper;
            _settlementStatusPublisher = settlementStatusPublisher;
            _settings = settings;

            _log = logFactory.CreateLog(this);
            _timer = new Timer(settings.Interval.TotalMilliseconds);
        }

        public async Task AddToQueueIfTransferred(string transactionHash, decimal fee)
        {
            try
            {
                IEnumerable<IPaymentRequest> paymentRequests =
                    (await _paymentRequestsRepository.GetByTransferToMarketTransactionHash(transactionHash)).ToArray();
                if (!paymentRequests.Any())
                {
                    return;
                }

                decimal total = paymentRequests.Sum(r => r.PaidAmount);
                var tasks = new List<Task>();
                foreach (IPaymentRequest paymentRequest in paymentRequests)
                {
                    tasks.Add(AddToQueueIfTransferred(paymentRequest, total, fee));
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        public async Task AddToQueueIfTransferred(IPaymentRequest paymentRequest, decimal total, decimal fee)
        {
            try
            {
                decimal marketAmount = paymentRequest.PaidAmount - paymentRequest.PaidAmount * fee / total;
                marketAmount = (decimal) _accuracyRoundHelper.Round((double) marketAmount,
                    paymentRequest.PaymentAssetId);

                await _paymentRequestsRepository.SetTransferredToMarketAsync(
                    paymentRequest.Id, marketAmount, fee);

                AssetPair assetPair = _assetPairService.GetAssetPair(paymentRequest.PaymentAssetId,
                    paymentRequest.SettlementAssetId);

                await _tradeOrdersRepository.InsertOrMergeTradeOrderAsync(new TradeOrder()
                {
                    PaymentRequestId = paymentRequest.Id,
                    Volume = marketAmount,
                    AssetPairId = assetPair.Id,
                    PaymentAssetId = paymentRequest.PaymentAssetId,
                    SettlementAssetId = paymentRequest.SettlementAssetId,
                    OrderAction =
                        string.Equals(assetPair.BaseAssetId, paymentRequest.PaymentAssetId,
                            StringComparison.OrdinalIgnoreCase)
                            ? OrderAction.Sell
                            : OrderAction.Buy
                });

                await _settlementStatusPublisher.PublishAsync(paymentRequest);
            }
            catch (ArgumentException ex)
            {
                _log.Error(ex, "Payment request can not be settled.",
                    new {PaymentRequestId = paymentRequest.Id});
            }
        }

        public void Start()
        {
            _timer.Elapsed += async (sender, e) => await TradeAsync();
            _timer.Start();
        }

        private async Task TradeAsync()
        {
            try
            {
                ITradeOrder[] tradeOrders = (await _tradeOrdersRepository.GetAsync()).ToArray();
                if (!tradeOrders.Any())
                {
                    return;
                }

                await _lykkeBalanceService.GetFromServerAsync();

                foreach (ITradeOrder tradeOrder in tradeOrders)
                {
                    await TradeAsync(tradeOrder);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        private async Task TradeAsync(ITradeOrder tradeOrder)
        {
            try
            {
                decimal balance = _lykkeBalanceService.GetAssetBalance(tradeOrder.PaymentAssetId);
                if (balance < tradeOrder.Volume)
                {
                    _log.Error(null,$"There is not enought balance of {tradeOrder.PaymentAssetId}. " +
                                    $"Required volume is {tradeOrder.Volume}. Balance is {balance}.",
                        new { tradeOrder.PaymentRequestId });
                    return;
                }

                MarketOrderModel model = new MarketOrderModel()
                {
                    Id = Guid.NewGuid().ToString(),
                    AssetPairId = tradeOrder.AssetPairId,
                    ClientId = _settings.ClientId,
                    OrderAction = OrderAction.Sell,
                    Straight = tradeOrder.OrderAction == OrderAction.Sell,
                    Volume = (double)tradeOrder.Volume
                };

                _log.Info($"Handling market order:\r\n{model.ToJson()}",
                    new { tradeOrder.PaymentRequestId });

                MarketOrderResponse response = await _matchingEngineClient.HandleMarketOrderAsync(model);
                if (response.Status != MeStatusCodes.Ok)
                {
                    _log.Warning($"Can not handle market order:\r\n{model.ToJson()}\r\n" +
                                 $"Response: {response.ToJson()}", null,
                        new { tradeOrder.PaymentRequestId }); 
                    return;
                }

                _log.Info($"Handled market order:\r\n{model.ToJson()}\r\n" +
                          $"Response: {response.ToJson()}",
                    new { tradeOrder.PaymentRequestId });

                _lykkeBalanceService.AddAsset(tradeOrder.PaymentAssetId, tradeOrder.Volume);
                _lykkeBalanceService.AddAsset(tradeOrder.SettlementAssetId, tradeOrder.Volume * (decimal)response.Price);
                await _tradeOrdersRepository.DeleteAsync(tradeOrder);
                IPaymentRequest paymentRequest = await _paymentRequestsRepository.SetExchangedAsync(tradeOrder.PaymentRequestId,
                    (decimal) response.Price, model.Id);
                await _transferToMerchantService.AddToQueue(tradeOrder.PaymentRequestId);
                await _settlementStatusPublisher.PublishAsync(paymentRequest);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }        

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public void Stop()
        {
            _timer?.Stop();
        }
    }
}
