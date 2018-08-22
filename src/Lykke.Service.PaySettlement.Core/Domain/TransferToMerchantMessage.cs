namespace Lykke.Service.PaySettlement.Core.Domain
{
    public class TransferToMerchantMessage
    {
        public string PaymentRequestId { get; set; }
        public string MerchantClientId { get; set; }
        public string AssetId { get; set; }
        public decimal Amount { get; set; }
    }
}
