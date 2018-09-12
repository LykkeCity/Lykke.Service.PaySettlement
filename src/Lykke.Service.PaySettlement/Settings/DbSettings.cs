using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.PaySettlement.Settings
{
    public class DbSettings
    {
        [AzureTableCheck]
        public string LogsConnString { get; set; }

        [AzureTableCheck]
        public string DataConnString { get; set; }

        public string PaymentRequestsTableName { get; set; }

        public string ExchangeOrdersTableName { get; set; }

        public string TransferToMarketQueue { get; set; }
    }
}
