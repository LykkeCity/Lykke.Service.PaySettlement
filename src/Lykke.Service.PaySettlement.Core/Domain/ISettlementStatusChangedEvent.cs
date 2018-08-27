namespace Lykke.Service.PaySettlement.Core.Domain
{
    public interface ISettlementStatusChangedEvent
    {
        string PaymentRequestId { get; }
        SettlementStatus SettlementStatus { get; }
        string TransferToMarketTransactionHash { get; }
        decimal TransferToMarketTransactionFee { get; }
        decimal MarketAmount { get; }
        decimal MarketPrice { get; }        
        string MarketOrderId { get; }
        decimal TransferredAmount { get; }
        string MerchantClientId { get; }
    }
}
