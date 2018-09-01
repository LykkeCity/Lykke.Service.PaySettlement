using JetBrains.Annotations;
using Lykke.Service.PaySettlement.Core.Settings;

namespace Lykke.Service.PaySettlement.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class PaySettlementSettings
    {
        public DbSettings Db { get; set; }

        public RabbitMqSubscriberSettings PaymentRequestsSubscriber { get; set; }

        public RabbitMqPublisherSettings SettlementStatusPublisher { get; set; }

        public TradeServiceSettings TradeService { get; set; }

        public TransferToMarketServiceSettings TransferToMarketService { get; set; }

        public AssetServiceSettings AssetService { get; set; }

        public TransferToMerchantServiceSettings TransferToMerchantService { get; set; }

        public string ClientId { get; set; }

        public string CqrsEnvironment { get; set; }

        public bool IsMainNet { get; set; }
    }
}
