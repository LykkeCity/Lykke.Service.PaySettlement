using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.PaySettlement.Settings
{
    public class RabbitMqPublisherSettings
    {
        [AmqpCheck]
        public string ConnectionString { get; set; }

        public string ExchangeName { get; set; }
    }
}
