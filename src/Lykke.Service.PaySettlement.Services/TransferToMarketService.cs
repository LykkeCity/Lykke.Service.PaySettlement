using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.PayInternal.Client;
using Lykke.Service.PayInternal.Client.Models.PaymentRequest;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Core.Services;
using Lykke.Service.PaySettlement.Core.Settings;
using System;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.PaySettlement.Core.Repositories;

namespace Lykke.Service.PaySettlement.Services
{
    public class TransferToMarketService : ITransferToMarketService
    {
        private readonly ITransferToMarketQueue _transferToMarketQueue;        
        private readonly IPayInternalClient _payInternalClient;
        private readonly TransferToMarketServiceSettings _settings;        
        private readonly ILog _log;

        public TransferToMarketService(ITransferToMarketQueue transferToMarketQueue,
            IPayInternalClient payInternalClient, TransferToMarketServiceSettings settings, 
            ILogFactory logFactory)
        {
            _transferToMarketQueue = transferToMarketQueue;
            _payInternalClient = payInternalClient;
            _settings = settings;
            _log = logFactory.CreateLog(this);
        }

        public Task AddToQueueAsync(IPaymentRequest paymentRequest)
        {
            return _transferToMarketQueue.AddPaymentRequestsAsync(paymentRequest);
        }

        public async Task<TransferBatchPaymentRequestsResult> TransferBatchPaymentRequestsAsync()
        {
           return await _transferToMarketQueue.ProcessTransferAsync(
               TransferBatchPaymentRequestsAsync, _settings.MaxTransfersPerTransaction);
        }

        private async Task<TransferBatchPaymentRequestsResult> TransferBatchPaymentRequestsAsync(
            TransferToMarketMessage[] messages)
        {
            try
            {
                if (!messages.Any())
                {
                    _log.Info("There are no payment requests for processing.");
                    return new TransferBatchPaymentRequestsResult {IsSuccess = true};
                }

                var transferRequest = new BtcFreeTransferRequest()
                {
                    DestAddress = _settings.MultisigWalletAddress,
                    Sources = messages.Select(m => new BtcTransferSourceInfo
                    {
                        Address = m.PaymentRequestWalletAddress,
                        Amount = m.Amount
                    }).ToArray()
                };

                BtcTransferResponse response = await _payInternalClient.BtcFreeTransferAsync(transferRequest);
                _log.Info(
                    $"Transfer from Lykke Pay wallets to market multisig wallet {_settings.MultisigWalletAddress}. " +
                    $"TransactionHash: {response.Hash}. Payment requests:\r\n" +
                    $"{messages.ToJson()}");

                return new TransferBatchPaymentRequestsResult
                {
                    IsSuccess = true,
                    TransactionHash = response.Hash,
                    PaymentRequests = messages.Cast<PaymentRequestIdentifier>().ToArray()
                };
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Unknown error has occured on transferring: {messages.ToJson()}");

                return new TransferBatchPaymentRequestsResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Unknown error has occured on transferring.",
                    PaymentRequests = messages.Cast<PaymentRequestIdentifier>().ToArray()
                };
            }
        }
    }
}
