using AzureStorage;
using Lykke.Service.PaySettlement.Core.Domain;
using System.Collections.Generic;
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

        public Task DeleteAsync(ITradeOrder tradeOrder)
        {
            return _storage.DeleteIfExistAsync(TradeOrderEntity.GetPartitionKey(tradeOrder.AssetPairId),
                TradeOrderEntity.GetRowKey(tradeOrder.PaymentRequestId));
        }
    }
}
