using System;

namespace Lykke.Service.PaySettlement.Core.Domain
{
    public class TransferBatchPaymentRequestsResult : IMessageProcessorResult
    {
        public bool IsSuccess { get; set; }

        public string ErrorMessage { get; set; }

        public Exception Exception { get; set; }

        public string TransactionHash { get; set; }

        public string DestinationAddress { get; set; }

        public decimal TransactionAmount { get; set; }

        public string TransactionAssetId { get; set; }

        public IPaymentRequestIdentifier[] PaymentRequests { get; set; }
    }
}
