namespace Lykke.Service.PaySettlement.Core.Domain
{
    public class TransferBatchPaymentRequestsResult : IMessageProcessorResult
    {
        public bool IsSuccess { get; set; }

        public string ErrorMessage { get; set; }

        public string TransactionHash { get; set; }

        public IPaymentRequestIdentifier[] PaymentRequests { get; set; }
    }
}
