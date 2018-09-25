namespace Lykke.Service.PaySettlement.Core.Domain
{
    public class PaymentRequestIdentifier : IPaymentRequestIdentifier
    {
        public string MerchantId { get; set; }
        public string PaymentRequestId { get; set; }
    }
}
