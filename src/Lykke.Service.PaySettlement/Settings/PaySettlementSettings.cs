using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Service.PaySettlement.Core.Settings;

namespace Lykke.Service.PaySettlement.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class PaySettlementSettings
    {
        public DbSettings Db { get; set; }

        public ChaosSettings ChaosKitty { get; set; }

        public RabbitMqSubscriberSettings PaymentRequestsSubscriber { get; set; }

        public RabbitMqPublisherSettings SettlementStatusPublisher { get; set; }

        public TradeServiceSettings TradeService { get; set; }

        public TransferToMarketServiceSettings TransferToMarketService { get; set; }

        public TransferToMerchantServiceSettings TransferToMerchantService { get; set; }

        public bool IsMainNet { get; set; }
    }
}
