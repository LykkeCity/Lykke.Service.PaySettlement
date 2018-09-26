namespace Lykke.Service.PaySettlement.Core.Domain
{
    public class TransferToMerchantResult : PaymentRequestIdentifier
    {
        public SettlementProcessingError Error { get; set; }

        public string ErrorMessage { get; set; }

        public decimal Amount { get; set; }

        public string AssetId { get; set; }
    }
}
