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

        public TransferToMarketMessage[] TransferToMarketMessages { get; set; }
    }
}
