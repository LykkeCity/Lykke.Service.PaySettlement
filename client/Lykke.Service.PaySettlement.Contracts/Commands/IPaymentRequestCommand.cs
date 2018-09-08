namespace Lykke.Service.PaySettlement.Contracts.Commands
{
    public interface IPaymentRequestCommand
    {
        string MerchantId { get; }

        string PaymentRequestId { get; }
    }
}
