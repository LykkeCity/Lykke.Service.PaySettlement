using System;
using System.Linq;
using AzureStorage;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.AzureRepositories.Trading
{
    public class ExchangeOrdersRepository : ITradeOrdersRepository
    {
        private readonly INoSQLTableStorage<ExchangeOrderEntity> _storage;

        public ExchangeOrdersRepository(INoSQLTableStorage<ExchangeOrderEntity> storage)
        {
            _storage = storage;
        }

        public Task InsertOrReplaceAsync(IExchangeOrder exchangeOrder)
        {
            return _storage.InsertOrReplaceAsync(new ExchangeOrderEntity(exchangeOrder));
        }

        public async Task<IExchangeOrder> GetTopOrderAsync(DateTime lastAttemptUtc)
        {
            var query = new TableQuery<ExchangeOrderEntity>()
                .Where(TableQuery.GenerateFilterConditionForDate(nameof(ExchangeOrderEntity.LastAttemptUtc),
                    QueryComparisons.LessThanOrEqual, lastAttemptUtc));

            return await _storage.GetTopRecordAsync(query);
        }

        public async Task<IExchangeOrder> SetLastAttemptAsync(string assetPairId, string paymentRequestId,
            DateTime lastAttemptUtc)
        {
            return await _storage.MergeAsync(ExchangeOrderEntity.GetPartitionKey(assetPairId),
                ExchangeOrderEntity.GetRowKey(paymentRequestId), o =>
                {
                    o.LastAttemptUtc = lastAttemptUtc;
                    return o;
                });
        }

        public Task DeleteAsync(IExchangeOrder exchangeOrder)
        {
            return _storage.DeleteIfExistAsync(ExchangeOrderEntity.GetPartitionKey(exchangeOrder.AssetPairId),
                ExchangeOrderEntity.GetRowKey(exchangeOrder.PaymentRequestId));
        }
    }
}
