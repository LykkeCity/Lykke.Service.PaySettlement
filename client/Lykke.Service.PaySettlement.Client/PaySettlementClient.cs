using Lykke.HttpClientGenerator;

namespace Lykke.Service.PaySettlement.Client
{
    /// <summary>
    /// PaySettlement API aggregating interface.
    /// </summary>
    public class PaySettlementClient : IPaySettlementClient
    {
        // Note: Add similar Api properties for each new service controller

        /// <summary>Inerface to PaySettlement Api.</summary>
        public IPaySettlementApi Api { get; private set; }

        /// <summary>C-tor</summary>
        public PaySettlementClient(IHttpClientGenerator httpClientGenerator)
        {
            Api = httpClientGenerator.Generate<IPaySettlementApi>();
        }
    }
}
