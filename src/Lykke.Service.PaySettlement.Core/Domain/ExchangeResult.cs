namespace Lykke.Service.PaySettlement.Core.Domain
{
    public class ExchangeResult : PaymentRequestIdentifier
    {
        public bool IsSuccess { get; set; }

        public bool CanBeRetried { get; set; }

        public string ErrorMessage { get; set; }

        public decimal MarketPrice { get; set; }

        public string MarketOrderId { get; set; }

        public string AssetPairId { get; set; }
    }
}
