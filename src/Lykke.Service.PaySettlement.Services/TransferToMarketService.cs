using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.PayInternal.Client;
using Lykke.Service.PayInternal.Client.Models.PaymentRequest;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Core.Services;
using Lykke.Service.PaySettlement.Core.Settings;
using System;
using System.Collections.Generic;
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
            BtcFreeTransferRequest transferRequest = null;
            try
            {
                if (!messages.Any())
                {
                    return new TransferBatchPaymentRequestsResult {IsSuccess = true};
                }

                transferRequest = new BtcFreeTransferRequest()
                {
                    DestAddress = _settings.MultisigWalletAddress,
                    Sources = messages.Distinct(new TransferToMarketMessageEqualityComparer())
                        .Select(m => new BtcTransferSourceInfo
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
                    PaymentRequests = messages.Cast<IPaymentRequestIdentifier>().ToArray()
                };
            }
            catch (Exception ex)
            {
                string errorMessage;
                if (transferRequest != null)
                {
                    errorMessage = $"Unknown error has occured on transferring: {transferRequest.ToJson()}";
                }
                else
                {
                    errorMessage = $"Unknown error has occured on transferring: {messages.ToJson()}";
                }

                return new TransferBatchPaymentRequestsResult
                {
                    IsSuccess = false,
                    Exception = ex,
                    ErrorMessage = errorMessage,
                    PaymentRequests = messages.Cast<IPaymentRequestIdentifier>().ToArray()
                };
            }
        }
    }
}
