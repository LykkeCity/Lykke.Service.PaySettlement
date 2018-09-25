namespace Lykke.Service.PaySettlement.Core.Domain
{
    public class TransferToMarketMessage : PaymentRequestIdentifier
    {
        public string PaymentRequestWalletAddress { get; set; }
        public decimal Amount { get; set; }

        public TransferToMarketMessage()
        {
        }

        public TransferToMarketMessage(IPaymentRequest paymentRequest)
        {
            MerchantId = paymentRequest.MerchantId;
            PaymentRequestId = paymentRequest.PaymentRequestId;
            PaymentRequestWalletAddress = paymentRequest.WalletAddress;
            Amount = paymentRequest.PaidAmount;
        }
    }
}
