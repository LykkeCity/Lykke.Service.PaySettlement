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

namespace Lykke.Service.PaySettlement.Services
{
    public class TradeService : TimerPeriod, ITradeService
    {
        private readonly IMatchingEngineClient _matchingEngineClient;
        private readonly IPaymentRequestsRepository _paymentRequestsRepository;
        private readonly ITradeOrdersRepository _tradeOrdersRepository;
        private readonly IAssetService _assetService;
        private readonly ILykkeBalanceService _lykkeBalanceService;             
        private readonly IStatusService _statusService;
        private readonly string _clientId;
        private readonly ILog _log;

        public TradeService(IMatchingEngineClient matchingEngineClient, 
            IPaymentRequestsRepository paymentRequestsRepository, 
            ITradeOrdersRepository tradeOrdersRepository, IAssetService assetService, 
            ILykkeBalanceService lykkeBalanceService, IStatusService statusService, 
            string clientId, ILogFactory logFactory, TradeServiceSettings settings)
            : base(settings.Interval, logFactory)
        {
            _matchingEngineClient = matchingEngineClient;
            _paymentRequestsRepository = paymentRequestsRepository;
            _tradeOrdersRepository = tradeOrdersRepository;
            _assetService = assetService;
            _lykkeBalanceService = lykkeBalanceService;                        
            _statusService = statusService;
            _clientId = clientId;

            _log = logFactory.CreateLog(this);
        }

        public async Task AddToQueueIfTransferredAsync(string transactionHash, decimal fee)
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
                tasks.Add(AddToQueueIfTransferredAsync(paymentRequest, total, fee));
            }

            await Task.WhenAll(tasks);
        }

        private async Task AddToQueueIfTransferredAsync(IPaymentRequest paymentRequest, decimal total, decimal fee)
        {
            try
            {
                AssetPair assetPair = _assetService.GetAssetPair(paymentRequest.PaymentAssetId,
                    paymentRequest.SettlementAssetId);

                OrderAction orderAction = string.Equals(assetPair.BaseAssetId, paymentRequest.PaymentAssetId,
                    StringComparison.OrdinalIgnoreCase)
                    ? OrderAction.Sell
                    : OrderAction.Buy;

                decimal marketAmount = paymentRequest.PaidAmount - paymentRequest.PaidAmount * fee / total;
                marketAmount = marketAmount.TruncateDecimalPlaces(IsStraight(orderAction)
                        ? assetPair.InvertedAccuracy
                        : assetPair.Accuracy);

                await _statusService.SetTransferredToMarketAsync(new TradeOrder()
                {
                    MerchantId = paymentRequest.MerchantId,
                    PaymentRequestId = paymentRequest.Id,
                    Volume = marketAmount,
                    AssetPairId = assetPair.Id,
                    PaymentAssetId = paymentRequest.PaymentAssetId,
                    SettlementAssetId = paymentRequest.SettlementAssetId,
                    OrderAction = orderAction
                }, fee);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Payment request can not be settled.",
                    new
                    {
                        paymentRequest.MerchantId,
                        PaymentRequestId = paymentRequest.Id
                    });
                await _statusService.SetErrorAsync(paymentRequest.MerchantId, paymentRequest.Id,
                    $"Can not add to trade queue: {ex}.");
            }
        }

        public override async Task Execute()
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

        private async Task TradeAsync(ITradeOrder tradeOrder)
        {
            try
            {
                decimal balance = _lykkeBalanceService.GetAssetBalance(tradeOrder.PaymentAssetId);
                if (balance < tradeOrder.Volume)
                {
                    _log.Error(null,$"There is not enought balance of {tradeOrder.PaymentAssetId}. " +
                                    $"Required volume is {tradeOrder.Volume}. Balance is {balance}.",
                        new
                        {
                            tradeOrder.MerchantId,
                            tradeOrder.PaymentRequestId
                        });
                    return;
                }

                var model = new MarketOrderModel()
                {
                    Id = Guid.NewGuid().ToString(),
                    AssetPairId = tradeOrder.AssetPairId,
                    ClientId = _clientId,
                    OrderAction = OrderAction.Sell,
                    Straight = IsStraight(tradeOrder.OrderAction),
                    Volume = (double)tradeOrder.Volume
                };

                _log.Info($"Handling market order:\r\n{model.ToJson()}",
                    new
                    {
                        tradeOrder.MerchantId,
                        tradeOrder.PaymentRequestId
                    });

                MarketOrderResponse response = await _matchingEngineClient.HandleMarketOrderAsync(model);
                if (response.Status != MeStatusCodes.Ok)
                {
                    _log.Warning($"Can not handle market order:\r\n{model.ToJson()}\r\n" +
                                 $"Response: {response.ToJson()}", null,
                        new
                        {
                            tradeOrder.MerchantId,
                            tradeOrder.PaymentRequestId
                        }); 
                    return;
                }

                _log.Info($"Handled market order:\r\n{model.ToJson()}\r\n" +
                          $"Response: {response.ToJson()}",
                    new
                    {
                        tradeOrder.MerchantId,
                        tradeOrder.PaymentRequestId
                    });

                _lykkeBalanceService.AddAsset(tradeOrder.PaymentAssetId, -tradeOrder.Volume);
                _lykkeBalanceService.AddAsset(tradeOrder.SettlementAssetId, tradeOrder.Volume * (decimal)response.Price);
                await _tradeOrdersRepository.DeleteAsync(tradeOrder);
                await _statusService.SetExchangedAsync(tradeOrder.MerchantId, tradeOrder.PaymentRequestId,
                    (decimal) response.Price, model.Id);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Can not exchange.", new
                {
                    tradeOrder.MerchantId,
                    tradeOrder.PaymentRequestId
                });
            }
        }

        private bool IsStraight(OrderAction orderAction)
        {
            return orderAction == OrderAction.Sell;
        }
    }
}

