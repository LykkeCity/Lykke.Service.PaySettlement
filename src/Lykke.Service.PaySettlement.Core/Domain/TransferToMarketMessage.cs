namespace Lykke.Service.PaySettlement.Core.Domain
{
    public class TransferToMarketMessage
    {
        public string PaymentRequestId { get; set; }
        public string PaymentRequestWalletAddress { get; set; }
        public decimal Amount { get; set; }

        public TransferToMarketMessage()
        {
        }

        public TransferToMarketMessage(IPaymentRequest paymentRequest)
        {
            PaymentRequestId = paymentRequest.Id;
            PaymentRequestWalletAddress = paymentRequest.WalletAddress;
            Amount = paymentRequest.PaidAmount;
        }
    }
}
