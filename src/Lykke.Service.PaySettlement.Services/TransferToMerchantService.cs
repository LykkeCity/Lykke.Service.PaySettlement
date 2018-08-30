using Autofac;
using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.MatchingEngine.Connector.Models.Api;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.ExchangeOperations.Client;
using Lykke.Service.ExchangeOperations.Client.AutorestClient.Models;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Core.Services;
using Lykke.Service.PaySettlement.Core.Settings;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace Lykke.Service.PaySettlement.Services
{
    public class TransferToMerchantService : IStartable, IStopable
    {
        private readonly ITransferToMerchantQueue _transferToMerchantQueue;
        private readonly ILog _log;        
        private readonly TransferToMerchantServiceSettings _settings;
        private readonly IAssetService _assetService;
        private readonly ILykkeBalanceService _lykkeBalanceService;
        private readonly IStatusService _statusService;
        private readonly IExchangeOperationsServiceClient _exchangeOperationsServiceClient;
        private readonly Timer _timer;

        public TransferToMerchantService(ITransferToMerchantQueue transferToMerchantQueue,
            TransferToMerchantServiceSettings settings, IAssetService assetService, 
            ILykkeBalanceService lykkeBalanceService, ILogFactory logFactory,
            IStatusService statusService, IExchangeOperationsServiceClient exchangeOperationsServiceClient)
        {
            _transferToMerchantQueue = transferToMerchantQueue;
            _log = logFactory.CreateLog(this);
            _settings = settings;
            _assetService = assetService;
            _lykkeBalanceService = lykkeBalanceService;
            _statusService = statusService;
            _exchangeOperationsServiceClient = exchangeOperationsServiceClient;

            _timer = new Timer(settings.Interval.TotalMilliseconds);            
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

                Asset asset = _assetService.GetAsset(message.AssetId);

                var request = new
                {
                    destClientId = message.MerchantClientId,
                    sourceClientId = _settings.ClientId,
                    amount = message.Amount.TruncateDecimalPlaces(asset.Accuracy),
                    assetId = message.AssetId
                };

                ExchangeOperationResult result = await _exchangeOperationsServiceClient.TransferAsync(
                    request.destClientId, request.sourceClientId, (double)request.amount, request.assetId);

                if (result.Code != (int)MeStatusCodes.Ok)
                {
                    _log.Error(null, $"Can not transfer to merchant:\r\n{request.ToJson()}\r\n" +
                                     $"Response: {result.ToJson()}", new {message.PaymentRequestId});
                    return false;
                }

                _lykkeBalanceService.AddAsset(message.AssetId, -request.amount);
                await _statusService.SetTransferredToMerchantAsync(message.MerchantId, 
                    message.PaymentRequestId, request.amount);

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
