using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.PaySettlement.Client 
{
    /// <summary>
    /// PaySettlement client settings.
    /// </summary>
    public class PaySettlementServiceClientSettings 
    {
        /// <summary>Service url.</summary>
        [HttpCheck("api/isalive")]
        public string ServiceUrl {get; set;}
    }
}
