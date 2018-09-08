using Lykke.SettingsReader.Attributes;
using NBitcoin;

namespace Lykke.Service.PaySettlement.Settings
{
    public class NinjaServiceClientSettings
    {
        [HttpCheck("/")]
        public string ServiceUrl { get; set; }        
    }
}
