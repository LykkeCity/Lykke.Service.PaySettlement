using AzureStorage;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.AzureRepositories.Trading
{
    public class TradeOrdersRepository : ITradeOrdersRepository
    {
        private readonly INoSQLTableStorage<ExchangeOrderEntity> _storage;

        public TradeOrdersRepository(INoSQLTableStorage<ExchangeOrderEntity> storage)
        {
            _storage = storage;
        }

        public Task InsertOrReplaceAsync(IExchangeOrder exchangeOrder)
        {
            return _storage.InsertOrReplaceAsync(new ExchangeOrderEntity(exchangeOrder));
        }

        public async Task<IExchangeOrder> GetTopOrderAsync()
        {
            return await _storage.GetTopRecordAsync(new TableQuery<ExchangeOrderEntity>());
        }

        public Task DeleteAsync(IExchangeOrder exchangeOrder)
        {
            return _storage.DeleteIfExistAsync(ExchangeOrderEntity.GetPartitionKey(exchangeOrder.AssetPairId),
                ExchangeOrderEntity.GetRowKey(exchangeOrder.PaymentRequestId));
        }
    }
}
