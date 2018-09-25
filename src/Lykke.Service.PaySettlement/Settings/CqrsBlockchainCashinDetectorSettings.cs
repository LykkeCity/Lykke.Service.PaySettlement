using Lykke.Messaging.Serialization;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.PaySettlement.Settings
{
    public class CqrsBlockchainCashinDetectorSettings
    {
        [AmqpCheck]
        public string ConnectionString { get; set; }

        public string Environment { get; set; }

        public string Messaging { get; set; }

        public SerializationFormat SerializationFormat { get; set; }

        public string EventsRoute { get; set; }
        
        public bool IsMainNet { get; set; }
    }
}
