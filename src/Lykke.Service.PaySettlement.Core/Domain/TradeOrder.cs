using Lykke.MatchingEngine.Connector.Models.Common;

namespace Lykke.Service.PaySettlement.Core.Domain
{
    public class TradeOrder : ITradeOrder
    {
        public string MerchantId { get; set; }

        public string PaymentRequestId { get; set; }

        public string AssetPairId { get; set; }

        public string PaymentAssetId { get; set; }

        public string SettlementAssetId { get; set; }

        public OrderAction OrderAction { get; set; }

        public decimal Volume { get; set; }
    }
}
