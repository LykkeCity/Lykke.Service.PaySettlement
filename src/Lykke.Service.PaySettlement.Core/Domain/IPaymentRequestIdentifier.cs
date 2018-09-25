namespace Lykke.Service.PaySettlement.Core.Domain
{
    public interface IPaymentRequestIdentifier
    {
        string MerchantId { get; }
        string PaymentRequestId { get; }
    }
}
