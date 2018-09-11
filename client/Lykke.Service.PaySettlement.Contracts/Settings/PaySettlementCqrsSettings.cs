using Lykke.SettingsReader.Attributes;
using Lykke.Messaging.Serialization;

namespace Lykke.Service.PaySettlement.Contracts.Settings
{
    public class PaySettlementCqrsSettings
    {
        [AmqpCheck]
        public string ConnectionString { get; set; }

        public string Environment { get; set; }

        public string Messaging { get; set; }

        public SerializationFormat SerializationFormat { get; set; }

        public string SettlementBoundedContext { get; set; }

        [Optional]
        public string EventsRoute { get; set; }

        [Optional]
        public string CommandsRoute { get; set; }
    }
}
