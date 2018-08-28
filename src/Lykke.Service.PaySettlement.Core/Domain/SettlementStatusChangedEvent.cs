namespace Lykke.Service.PaySettlement.Core.Domain
{
    public class SettlementStatusChangedEvent : ISettlementStatusChangedEvent
    {
        public string PaymentRequestId { get; set; }
        public SettlementStatus SettlementStatus { get; set; }
        public string TransferToMarketTransactionHash { get; set; }
        public decimal TransferToMarketTransactionFee { get; set; }
        public decimal MarketAmount { get; set; }
        public decimal MarketPrice { get; set; }
        public string MarketOrderId { get; set; }
        public decimal TransferredAmount { get; set; }
        public string MerchantClientId { get; set; }
        public bool Error { get; set; }
        public string ErrorDescription { get; set; }
    }
}
