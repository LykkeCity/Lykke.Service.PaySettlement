using System;
using Lykke.MatchingEngine.Connector.Models.Common;

namespace Lykke.Service.PaySettlement.Core.Domain
{
    public class ExchangeOrder : PaymentRequestIdentifier, IExchangeOrder
    {
        public string AssetPairId { get; set; }

        public string PaymentAssetId { get; set; }

        public string SettlementAssetId { get; set; }

        public OrderAction OrderAction { get; set; }

        public decimal Volume { get; set; }

        public DateTime LastAttemptUtc { get; set; }
    }
}
