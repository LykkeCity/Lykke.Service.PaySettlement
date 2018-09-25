using System;

namespace Lykke.Service.PaySettlement.Core.Domain
{
    public class ExchangeResult : PaymentRequestIdentifier
    {
        public bool CanBeRetried { get; set; }

        public SettlementProcessingError Error { get; set; }

        public string ErrorMessage { get; set; }

        public Exception Exception { get; set; }

        public decimal MarketPrice { get; set; }

        public string MarketOrderId { get; set; }

        public string AssetPairId { get; set; }
    }
}
