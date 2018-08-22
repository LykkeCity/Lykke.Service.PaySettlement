using AzureStorage;
using Lykke.Service.PaySettlement.Core.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.AzureRepositories.Trading
{
    public class TradeOrdersRepository : ITradeOrdersRepository
    {
        private readonly INoSQLTableStorage<TradeOrderEntity> _storage;

        public TradeOrdersRepository(INoSQLTableStorage<TradeOrderEntity> storage)
        {
            _storage = storage;
        }

        public Task InsertOrMergeTradeOrderAsync(ITradeOrder tradeOrder)
        {
            return _storage.InsertOrMergeAsync(new TradeOrderEntity(tradeOrder));
        }

        public async Task<IEnumerable<ITradeOrder>> GetAsync()
        {
            return await _storage.GetDataAsync();
        }

        public async Task<IEnumerable<ITradeOrder>> GetAsync(string assetPair)
        {
            return await _storage.GetDataAsync(TradeOrderEntity.GetPartitionKey(assetPair));
        }

        public Task DeleteAsync(ITradeOrder tradeOrder)
        {
            return _storage.DeleteAsync(new TradeOrderEntity(tradeOrder));
        }

        public Task DeleteAsync(IEnumerable<ITradeOrder> tradeOrders)
        {
            return _storage.DeleteAsync(tradeOrders.Select(o=> new TradeOrderEntity(o)));
        }
    }
}
