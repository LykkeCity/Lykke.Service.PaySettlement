using Lykke.SettingsReader.Attributes;
using System;

namespace Lykke.Service.PaySettlement.Settings
{
    public class AssetsServiceSettings
    {
        [HttpCheck("/api/isalive")]
        public string ServiceUrl { get; set; }

        public TimeSpan ExpirationPeriod { get; set; }
    }
}
