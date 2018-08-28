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
        private readonly ILog _log;

        public StatusService(IPaymentRequestsRepository paymentRequestsRepository,
            ISettlementStatusPublisher settlementStatusPublisher, ILogFactory logFactory)
        {
            _paymentRequestsRepository = paymentRequestsRepository;
            _settlementStatusPublisher = settlementStatusPublisher;
            _log = logFactory.CreateLog(this);
        }

        public async Task<IPaymentRequest> SetTransferToMarketQueuedAsync(string id)
        {
            var paymentRequest = await _paymentRequestsRepository.SetTransferToMarketQueuedAsync(id);
            await _settlementStatusPublisher.PublishAsync(paymentRequest);
            _log.Info($"Payment request status is changed to {SettlementStatus.TransferToMarketQueued}.", 
                new { PaymentRequestId = paymentRequest.Id });
            return paymentRequest;
        }

        public async Task<IPaymentRequest> SetTransferringToMarketAsync(string id, string transactionHash)
        {
            var paymentRequest = await _paymentRequestsRepository.SetTransferringToMarketAsync(id, 
                transactionHash);
            await _settlementStatusPublisher.PublishAsync(paymentRequest);
            _log.Info($"Payment request status is changed to {SettlementStatus.TransferringToMarket}.", 
                new {
                PaymentRequestId = paymentRequest.Id,
                TransactionHash = transactionHash,
            });
            return paymentRequest;
        }

        public async Task<IPaymentRequest> SetTransferredToMarketAsync(string id, decimal marketAmount,
            decimal transactionFee)
        {
            var paymentRequest = await _paymentRequestsRepository.SetTransferredToMarketAsync(id,
                marketAmount, transactionFee);
            await _settlementStatusPublisher.PublishAsync(paymentRequest);
            _log.Info($"Payment request status is changed to {SettlementStatus.TransferredToMarket}.", 
                new { PaymentRequestId = paymentRequest.Id });
            return paymentRequest;
        }

        public async Task<IPaymentRequest> SetExchangedAsync(string id, decimal marketPrice, 
            string marketOrderId)
        {
            var paymentRequest = await _paymentRequestsRepository.SetExchangedAsync(id,
                marketPrice, marketOrderId);
            await _settlementStatusPublisher.PublishAsync(paymentRequest);
            _log.Info($"Payment request status is changed to {SettlementStatus.Exchanged}.", 
                new { PaymentRequestId = paymentRequest.Id });
            return paymentRequest;
        }

        public async Task<IPaymentRequest> SetTransferredToMerchantAsync(string id, decimal transferredAmount)
        {
            var paymentRequest = await _paymentRequestsRepository.SetTransferredToMerchantAsync(id,
                transferredAmount);
            await _settlementStatusPublisher.PublishAsync(paymentRequest);
            _log.Info($"Payment request status is changed to {SettlementStatus.TransferredToMerchant}. " +
                      $"Transferred {transferredAmount} {paymentRequest.SettlementAssetId}.", 
                new { PaymentRequestId = paymentRequest.Id });
            return paymentRequest;
        }

        public async Task<IPaymentRequest> SetErrorAsync(string id, string errorDescription)
        {
            var paymentRequest = await _paymentRequestsRepository.SetErrorAsync(id,
                errorDescription);
            await _settlementStatusPublisher.PublishAsync(paymentRequest);
            _log.Info($"Payment request error is setted with description {errorDescription}.", 
                new { PaymentRequestId = paymentRequest.Id });
            return paymentRequest;
        }
    }
}
