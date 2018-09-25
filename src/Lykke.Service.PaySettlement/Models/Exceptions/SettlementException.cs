using Lykke.Service.PaySettlement.Core.Domain;
using System;

namespace Lykke.Service.PaySettlement.Models.Exceptions
{
    public class SettlementException: Exception
    {
        public string MerchantId { get; set; }
        public string PaymentRequestId { get; set; }
        public SettlementProcessingError Error { get; set; }

        public SettlementException(string merchantId, string paymentRequestId, SettlementProcessingError error) 
            : base()
        {
            MerchantId = merchantId;
            PaymentRequestId = paymentRequestId;
            Error = error;
        }

        public SettlementException(string merchantId, string paymentRequestId, SettlementProcessingError error, 
            string message) : base(message)
        {
            MerchantId = merchantId;
            PaymentRequestId = paymentRequestId;
            Error = error;
        }

        public SettlementException(string merchantId, string paymentRequestId, SettlementProcessingError error, 
            string message, Exception innerException) : base(message, innerException)
        {
            MerchantId = merchantId;
            PaymentRequestId = paymentRequestId;
            Error = error;
        }
    }
}
