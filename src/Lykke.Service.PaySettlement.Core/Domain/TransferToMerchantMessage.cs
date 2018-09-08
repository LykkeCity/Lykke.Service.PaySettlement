namespace Lykke.Service.PaySettlement.Core.Domain
{
    public class TransferToMerchantMessage
    {
        public string MerchantId { get; set; }
        public string PaymentRequestId { get; set; }
        public string MerchantClientId { get; set; }
        public string AssetId { get; set; }
        public decimal Amount { get; set; }

        public TransferToMerchantMessage()
        {
        }

        public TransferToMerchantMessage(IPaymentRequest paymentRequest)
        {
            MerchantId = paymentRequest.MerchantId;
            PaymentRequestId = paymentRequest.Id;
            MerchantClientId = paymentRequest.MerchantClientId;
            Amount = paymentRequest.Amount;
            AssetId = paymentRequest.SettlementAssetId;
        }
    }
}
