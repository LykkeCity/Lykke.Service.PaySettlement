using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.PaySettlement.Settings
{
    public class CqrsTxTransactionsSettings
    {
        [AmqpCheck]
        public string ConnectionString { get; set; }

        public string Environment { get; set; }

        public bool IsMainNet { get; set; }
    }
}
