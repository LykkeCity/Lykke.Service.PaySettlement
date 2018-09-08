using System;
using JetBrains.Annotations;
using Lykke.Service.PaySettlement.Core.Settings;

namespace Lykke.Service.PaySettlement.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class PaySettlementSettings
    {
        public DbSettings Db { get; set; }

        public RabbitMqSubscriberSettings PaymentRequestsSubscriber { get; set; }

        public TransferToMarketServiceSettings TransferToMarketService { get; set; }

        public AssetServiceSettings AssetService { get; set; }

        public CqrsTxTransactionsSettings CqrsTxTransactions { get; set; }

        public string CqrsEnvironment { get; set; }

        public string ClientId { get; set; }

        public TimeSpan LykkeBalanceUpdateInterval { get; set; }
        public TimeSpan TransferToMarketInterval { get; set; }
        public TimeSpan ExchangeInterval { get; set; }
    }
}
