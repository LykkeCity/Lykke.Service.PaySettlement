namespace Lykke.Service.PaySettlement.Core.Domain
{
    public enum SettlementStatus
    {
        None =0,
        TransferToMarketQueued = 1,
        TransferringToMarket = 2,
        TransferredToMarket = 3,
        Exchanged = 4,
        TransferredToMerchant = 5
    }
}
