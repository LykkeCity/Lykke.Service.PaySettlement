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

namespace Lykke.Service.PaySettlement.Services
{
    public class TransferToMerchantService : TimerPeriod
    {
        private readonly ITransferToMerchantQueue _transferToMerchantQueue;
        private readonly IAssetService _assetService;
        private readonly ILykkeBalanceService _lykkeBalanceService;
        private readonly IStatusService _statusService;
        private readonly IExchangeOperationsServiceClient _exchangeOperationsServiceClient;
        private readonly string _clientId;
        private readonly ILog _log;

        public TransferToMerchantService(ITransferToMerchantQueue transferToMerchantQueue,
            IAssetService assetService, ILykkeBalanceService lykkeBalanceService, 
            IStatusService statusService, IExchangeOperationsServiceClient exchangeOperationsServiceClient,
            string clientId, ILogFactory logFactory, TransferToMerchantServiceSettings settings)
            :base(settings.Interval, logFactory)
        {
            _transferToMerchantQueue = transferToMerchantQueue;
            _assetService = assetService;
            _lykkeBalanceService = lykkeBalanceService;
            _statusService = statusService;
            _exchangeOperationsServiceClient = exchangeOperationsServiceClient;
            _clientId = clientId;

            _log = logFactory.CreateLog(this);
        }

        public override async Task Execute()
        {
            await _lykkeBalanceService.GetFromServerAsync();

            bool exist = true;
            while (exist)
            {
                exist = await _transferToMerchantQueue.ProcessTransferAsync(TransferAsync);
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
                        new
                        {
                            message.MerchantId,
                            message.PaymentRequestId
                        });
                    return false;
                }

                Asset asset = _assetService.GetAsset(message.AssetId);

                var request = new
                {
                    destClientId = message.MerchantClientId,
                    sourceClientId = _clientId,
                    amount = message.Amount.TruncateDecimalPlaces(asset.Accuracy),
                    assetId = message.AssetId
                };

                ExchangeOperationResult result = await _exchangeOperationsServiceClient.TransferAsync(
                    request.destClientId, request.sourceClientId, (double)request.amount, request.assetId);

                if (result.Code != (int)MeStatusCodes.Ok)
                {
                    _log.Error(null, $"Can not transfer to merchant:\r\n{request.ToJson()}\r\n" +
                                     $"Response: {result.ToJson()}", new
                    {
                        message.MerchantId,
                        message.PaymentRequestId
                    });
                    return false;
                }

                _lykkeBalanceService.AddAsset(message.AssetId, -request.amount);
                await _statusService.SetTransferredToMerchantAsync(message.MerchantId, 
                    message.PaymentRequestId, request.amount);

                return true;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Transfer to merchant is failed.",
                    new
                    {
                        message.MerchantId,
                        message.PaymentRequestId
                    });
            }
            return false;
        }
    }
}
