namespace Lykke.Service.PaySettlement.Contracts
{
    public enum SettlementProcessingError
    {
        None = 0,
        Unknown = 1,
        LowBalanceForExchange = 2,
        LowBalanceForTransferToMerchant = 3,
        MerchantNotFound = 4,
        NoLiquidityForExchange = 5,
        ExchangeLeadToNegativeSpread = 6,
        NoTransactionDetails = 7,
    }
}
