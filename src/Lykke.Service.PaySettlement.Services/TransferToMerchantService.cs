using Autofac;
using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.MatchingEngine.Connector.Models.Api;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Core.Services;
using Lykke.Service.PaySettlement.Core.Settings;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace Lykke.Service.PaySettlement.Services
{
    public class TransferToMerchantService : IStartable, IStopable, ITransferToMerchantService
    {
        private readonly ITransferToMerchantQueue _transferToMerchantQueue;
        private readonly IPaymentRequestsRepository _paymentRequestsRepository;
        private readonly IMatchingEngineClient _matchingEngineClient;
        private readonly ILog _log;
        private readonly Timer _timer;
        private readonly TransferToMerchantServiceSettings _settings;
        private readonly IAssetService _assetService;
        private readonly ILykkeBalanceService _lykkeBalanceService;

        public TransferToMerchantService(ITransferToMerchantQueue transferToMerchantQueue,
            IPaymentRequestsRepository paymentRequestsRepository,
            IMatchingEngineClient matchingEngineClient, TransferToMerchantServiceSettings settings,
            IAssetService assetService, ILykkeBalanceService lykkeBalanceService, ILogFactory logFactory)
        {
            _transferToMerchantQueue = transferToMerchantQueue;
            _paymentRequestsRepository = paymentRequestsRepository;
            _log = logFactory.CreateLog(this);
            _matchingEngineClient = matchingEngineClient;
            _settings = settings;
            _assetService = assetService;
            _timer = new Timer(settings.Interval.TotalMilliseconds);
            _lykkeBalanceService = lykkeBalanceService;
        }

        public async Task AddToQueue(string paymentRequestId)
        {
            try
            {
                IPaymentRequest paymentRequest = await _paymentRequestsRepository.GetAsync(paymentRequestId);
                if (paymentRequest == null)
                {
                    _log.Error(null, $"Payment request {paymentRequestId} is not found.",
                        new {PaymentRequestId = paymentRequestId});
                    return;
                }

                await _transferToMerchantQueue.AddAsync(new TransferToMerchantMessage()
                {
                    PaymentRequestId = paymentRequestId,
                    MerchantClientId = paymentRequest.MerchantClientId,
                    Amount = paymentRequest.MarketAmount * paymentRequest.MarketPrice,
                    AssetId = paymentRequest.SettlementAssetId
                });
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        public void Start()
        {
            _timer.Elapsed += async (sender, e) => await TransferAsync();
            _timer.Start();
        }

        private async Task TransferAsync()
        {
            try
            {
                await _lykkeBalanceService.GetFromServerAsync();

                bool exist = true;
                while (exist)
                {
                    exist = await _transferToMerchantQueue.ProcessTransferAsync(TransferAsync);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        private async Task<bool> TransferAsync(TransferToMerchantMessage message)
        {
            try
            {
                decimal balance = _lykkeBalanceService.GetAssetBalance(message.AssetId);
                if (balance < message.Amount)
                {
                    _log.Error(null, $"There is not enought balance of {message.AssetId}. " +
                                     $"Required volume is {message.Amount}. Balance is {balance}.",
                        new { message.PaymentRequestId });
                    return false;
                }

                string transferId = Guid.NewGuid().ToString();
                Asset asset = _assetService.GetAsset(message.AssetId);

                double factor = Math.Pow(10, asset.Accuracy);
                double amount = Math.Floor((double) message.Amount * factor) / factor;
                var request = new
                {
                    id = transferId,
                    fromClientId = _settings.ClientId,
                    toClientId = message.MerchantClientId,
                    assetId = message.AssetId,
                    asset.Accuracy,
                    amount = amount,
                    feeModel = (FeeModel) null,
                    overdraft = 0
                };

                MeResponseModel response = await _matchingEngineClient.TransferAsync(request.id,
                    request.fromClientId, request.toClientId, request.assetId,
                    request.Accuracy, request.amount, request.feeModel, request.overdraft);

                if (response.Status != MeStatusCodes.Ok)
                {
                    _log.Error(null, $"Can not transfer to merchant:\r\n{request.ToJson()}\r\n" +
                                     $"Response: {response.ToJson()}", new {message.PaymentRequestId});
                    return false;
                }

                _lykkeBalanceService.AddAsset(message.AssetId, (decimal)amount);
                await _paymentRequestsRepository.SetTransferredToMerchantAsync(message.PaymentRequestId);

                _log.Info($"Settelment is completed. Transferred {amount} {message.AssetId}", 
                    new { message.PaymentRequestId });

                return true;
            }
            catch (Exception ex)
            {
                _log.Error(ex);                
            }
            return false;
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
