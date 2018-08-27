using JetBrains.Annotations;
using Lykke.Sdk.Settings;
using Lykke.Service.Assets.Client;
using Lykke.Service.Balances.Client;
using Lykke.Service.PayInternal.Client;
using Lykke.Service.PayMerchant.Client;

namespace Lykke.Service.PaySettlement.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class AppSettings : BaseAppSettings
    {
        public PaySettlementSettings PaySettlementService { get; set; }

        public PayInternalServiceClientSettings PayInternalServiceClient { get; set; }
        public PayMerchantServiceClientSettings PayMerchantServiceClient { get; set; }
        public AssetsServiceSettings AssetsServiceClient { get; set; }
        public MatchingEngineSettings MatchingEngineClient { get; set; }
        public NinjaServiceClientSettings NinjaServiceClient { get; set; }
        public BalancesServiceClientSettings BalancesServiceClient { get; set; }
    }
}
