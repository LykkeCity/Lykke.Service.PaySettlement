using Lykke.MatchingEngine.Connector.Models.Common;

namespace Lykke.Service.PaySettlement.Core.Domain
{
    public interface ITradeOrder
    {
        string MerchantId { get; }

        string PaymentRequestId { get; }

        string AssetPairId { get; }

        string PaymentAssetId { get; }

        string SettlementAssetId { get; }

        OrderAction OrderAction { get; }

        decimal Volume { get; }
    }
}
