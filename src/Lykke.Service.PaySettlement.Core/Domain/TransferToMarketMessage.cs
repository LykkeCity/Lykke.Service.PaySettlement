namespace Lykke.Service.PaySettlement.Core.Domain
{
    public class TransferToMarketMessage
    {
        public string MerchantId { get; set; }
        public string PaymentRequestId { get; set; }
        public string PaymentRequestWalletAddress { get; set; }
        public decimal Amount { get; set; }

        public TransferToMarketMessage()
        {
        }

        public TransferToMarketMessage(IPaymentRequest paymentRequest)
        {
            MerchantId = paymentRequest.MerchantId;
            PaymentRequestId = paymentRequest.Id;
            PaymentRequestWalletAddress = paymentRequest.WalletAddress;
            Amount = paymentRequest.PaidAmount;
        }
    }
}
