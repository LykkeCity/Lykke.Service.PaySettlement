using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Core.Services;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Services
{
    public class StatusService : IStatusService
    {
        private readonly IPaymentRequestsRepository _paymentRequestsRepository;
        private readonly ISettlementStatusPublisher _settlementStatusPublisher;
        private readonly ITransferToMarketQueue _transferToMarketQueue;
        private readonly ITradeOrdersRepository _tradeOrdersRepository;
        private readonly ITransferToMerchantQueue _transferToMerchantQueue;
        private readonly ILog _log;

        public StatusService(IPaymentRequestsRepository paymentRequestsRepository,
            ISettlementStatusPublisher settlementStatusPublisher, 
            ITransferToMarketQueue transferToMarketQueue, ITradeOrdersRepository tradeOrdersRepository,
            ITransferToMerchantQueue transferToMerchantQueue, ILogFactory logFactory)
        {
            _paymentRequestsRepository = paymentRequestsRepository;
            _settlementStatusPublisher = settlementStatusPublisher;
            _transferToMarketQueue = transferToMarketQueue;
            _tradeOrdersRepository = tradeOrdersRepository;
            _transferToMerchantQueue = transferToMerchantQueue;
            _log = logFactory.CreateLog(this);
        }

        public async Task<IPaymentRequest> SetTransferToMarketQueuedAsync(string merchantId, string id)
        {
            var paymentRequest = await _paymentRequestsRepository.SetTransferToMarketQueuedAsync(merchantId, 
                id);
            await _settlementStatusPublisher.PublishAsync(paymentRequest);
            await _transferToMarketQueue.AddPaymentRequestsAsync(paymentRequest);

            _log.Info($"Payment request status is changed to {SettlementStatus.TransferToMarketQueued}.",
                new {PaymentRequestId = paymentRequest.Id});
            return paymentRequest;
        }

        public async Task<IPaymentRequest> SetTransferringToMarketAsync(string merchantId, string id, 
            string transactionHash)
        {
            var paymentRequest = await _paymentRequestsRepository.SetTransferringToMarketAsync(merchantId, 
                id, transactionHash);
            await _settlementStatusPublisher.PublishAsync(paymentRequest);

            _log.Info($"Payment request status is changed to {SettlementStatus.TransferringToMarket}.", 
                new {
                PaymentRequestId = paymentRequest.Id,
                TransactionHash = transactionHash,
            });
            return paymentRequest;
        }

        public async Task<IPaymentRequest> SetTransferredToMarketAsync(ITradeOrder tradeOrder, 
            decimal transactionFee)
        {
            var paymentRequest = await _paymentRequestsRepository.SetTransferredToMarketAsync(
                tradeOrder.MerchantId, tradeOrder.PaymentRequestId, tradeOrder.Volume, transactionFee);
            await _settlementStatusPublisher.PublishAsync(paymentRequest);
            await _tradeOrdersRepository.InsertOrMergeTradeOrderAsync(tradeOrder);

            _log.Info($"Payment request status is changed to {SettlementStatus.TransferredToMarket}.", 
                new { PaymentRequestId = paymentRequest.Id });
            return paymentRequest;
        }

        public async Task<IPaymentRequest> SetExchangedAsync(string merchantId, string id, 
            decimal marketPrice, string marketOrderId)
        {
            var paymentRequest = await _paymentRequestsRepository.SetExchangedAsync(merchantId, id,
                marketPrice, marketOrderId);
            await _settlementStatusPublisher.PublishAsync(paymentRequest);
            await _transferToMerchantQueue.AddPaymentRequestsAsync(paymentRequest);

            _log.Info($"Payment request status is changed to {SettlementStatus.Exchanged}.", 
                new { PaymentRequestId = paymentRequest.Id });
            return paymentRequest;
        }

        public async Task<IPaymentRequest> SetTransferredToMerchantAsync(string merchantId, string id, 
            decimal transferredAmount)
        {
            var paymentRequest = await _paymentRequestsRepository.SetTransferredToMerchantAsync(merchantId,
                id, transferredAmount);
            await _settlementStatusPublisher.PublishAsync(paymentRequest);

            _log.Info($"Payment request status is changed to {SettlementStatus.TransferredToMerchant}. " +
                      $"Transferred {transferredAmount} {paymentRequest.SettlementAssetId}.", 
                new { PaymentRequestId = paymentRequest.Id });
            return paymentRequest;
        }

        public async Task<IPaymentRequest> SetErrorAsync(string merchantId, string id, 
            string errorDescription)
        {
            var paymentRequest = await _paymentRequestsRepository.SetErrorAsync(merchantId, id,
                errorDescription);
            await _settlementStatusPublisher.PublishAsync(paymentRequest);
            _log.Info($"Payment request error is setted with description {errorDescription}.", 
                new { PaymentRequestId = paymentRequest.Id });
            return paymentRequest;
        }
    }
}
