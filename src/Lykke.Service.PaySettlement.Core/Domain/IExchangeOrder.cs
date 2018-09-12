using System;
using Lykke.MatchingEngine.Connector.Models.Common;

namespace Lykke.Service.PaySettlement.Core.Domain
{
    public interface IExchangeOrder: IPaymentRequestIdentifier
    {
        string AssetPairId { get; }

        string PaymentAssetId { get; }

        string SettlementAssetId { get; }

        OrderAction OrderAction { get; }

        decimal Volume { get; }

        DateTime LastAttemptUtc { get; }
    }
}
