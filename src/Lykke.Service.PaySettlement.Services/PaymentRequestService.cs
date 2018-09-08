using System.Collections.Generic;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Core.Services;
using System.Threading.Tasks;
using Lykke.Service.PaySettlement.Core.Repositories;

namespace Lykke.Service.PaySettlement.Services
{
    public class PaymentRequestService : IPaymentRequestService
    {
        private readonly IPaymentRequestsRepository _paymentRequestsRepository;
        private readonly ILog _log;

        public PaymentRequestService(IPaymentRequestsRepository paymentRequestsRepository,
            ILogFactory logFactory)
        {
            _paymentRequestsRepository = paymentRequestsRepository;
            _log = logFactory.CreateLog(this);
        }

        public Task<IPaymentRequest> GetAsync(IPaymentRequestIdentifier paymentRequestIdentifier)
        {
            return GetAsync(paymentRequestIdentifier.MerchantId, paymentRequestIdentifier.PaymentRequestId);
        }

        public Task<IPaymentRequest> GetAsync(string merchantId, string paymentRequestId)
        {
            return _paymentRequestsRepository.GetAsync(merchantId, paymentRequestId);

        }

        public async Task AddAsync(IPaymentRequest paymentRequest)
        {
            await _paymentRequestsRepository.InsertOrReplaceAsync(paymentRequest);

            string message = "Payment request is added.";
            if (paymentRequest.Error)
            {
                message += $"\r\n With error: {paymentRequest.ErrorDescription}";
            }

            _log.Info(message, new
            {
                paymentRequest.MerchantId,
                paymentRequest.PaymentRequestId
            });
        }

        public async Task<IPaymentRequest> SetTransferToMarketQueuedAsync(string merchantId, string id)
        {
            var paymentRequest = await _paymentRequestsRepository.SetTransferToMarketQueuedAsync(merchantId,
                id);

            _log.Info($"Payment request status is changed to {SettlementStatus.TransferToMarketQueued}.",
                new
                {
                    paymentRequest.MerchantId,
                    paymentRequest.PaymentRequestId
                });

            return paymentRequest;
        }

        public async Task<IPaymentRequest> SetTransferringToMarketAsync(string merchantId, string id, 
            string transactionHash)
        {
            var paymentRequest = await _paymentRequestsRepository.SetTransferringToMarketAsync(merchantId, 
                id, transactionHash);

            _log.Info($"Payment request status is changed to {SettlementStatus.TransferringToMarket}.",
                new
                {
                    paymentRequest.MerchantId,
                    paymentRequest.PaymentRequestId,
                    TransactionHash = transactionHash
                });

            return paymentRequest;
        }

        public Task<IEnumerable<IPaymentRequest>> GetByTransferToMarketTransactionHash(string transactionHash)
        {
            return _paymentRequestsRepository.GetByTransferToMarketTransactionHash(transactionHash);
        }

        public async Task<IPaymentRequest> SetExchangeQueuedAsync(IExchangeOrder exchangeOrder, 
            decimal transactionFee)
        {
            var paymentRequest = await _paymentRequestsRepository.SetExchangeQueuedAsync(
                exchangeOrder.MerchantId, exchangeOrder.PaymentRequestId, exchangeOrder.Volume, transactionFee);

            _log.Info($"Payment request status is changed to {SettlementStatus.ExchangeQueued}.", 
                new
                {
                    paymentRequest.MerchantId,
                    paymentRequest.PaymentRequestId
                });
            return paymentRequest;
        }

        public async Task<IPaymentRequest> SetExchangedAsync(string merchantId, string id, 
            decimal marketPrice, string marketOrderId)
        {
            var paymentRequest = await _paymentRequestsRepository.SetExchangedAsync(merchantId, id,
                marketPrice, marketOrderId);

            _log.Info($"Payment request status is changed to {SettlementStatus.Exchanged}.", 
                new
                {
                    paymentRequest.MerchantId,
                    paymentRequest.PaymentRequestId
                });
            return paymentRequest;
        }

        public async Task<IPaymentRequest> SetTransferredToMerchantAsync(string merchantId, string id, 
            decimal transferredAmount)
        {
            var paymentRequest = await _paymentRequestsRepository.SetTransferredToMerchantAsync(merchantId,
                id, transferredAmount);

            _log.Info($"Payment request status is changed to {SettlementStatus.TransferredToMerchant}. " +
                      $"Transferred {transferredAmount} {paymentRequest.SettlementAssetId}.", 
                new
                {
                    paymentRequest.MerchantId,
                    paymentRequest.PaymentRequestId
                });
            return paymentRequest;
        }

        public async Task<IPaymentRequest> SetErrorAsync(string merchantId, string id, 
            string errorDescription)
        {
            var paymentRequest = await _paymentRequestsRepository.SetErrorAsync(merchantId, id,
                errorDescription);

            return paymentRequest;
        }
    }
}
