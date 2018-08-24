using System.Collections.Generic;
using System.Linq;
using AzureStorage;
using AzureStorage.Tables.Templates.Index;
using Lykke.Service.PaySettlement.Core.Domain;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.AzureRepositories.PaymentRequests
{
    public class PaymentRequestsRepository : IPaymentRequestsRepository
    {
        private readonly INoSQLTableStorage<PaymentRequestEntity> _storage;
        private readonly INoSQLTableStorage<AzureIndex> _indexByTransferToMarketTransactionHash;

        public PaymentRequestsRepository(
            INoSQLTableStorage<PaymentRequestEntity> storage,
            INoSQLTableStorage<AzureIndex> indexByTransferToMarketTransactionHash)
        {
            _storage = storage;
            _indexByTransferToMarketTransactionHash = indexByTransferToMarketTransactionHash;
        }

        public async Task<IPaymentRequest> GetAsync(string id)
        {
            return await _storage.GetDataAsync(PaymentRequestEntity.GetPartitionKey(),
                PaymentRequestEntity.GetRowKey(id));
        }

        public async Task<IEnumerable<IPaymentRequest>> GetByTransferToMarketTransactionHash(string transactionHash)
        {
            var index = await _indexByTransferToMarketTransactionHash.GetDataAsync(
                IndexByTransferToMarketTransactionHash.GeneratePartitionKey(transactionHash));
            return await _storage.GetDataAsync(index);
        }

        public Task InsertOrMergeAsync(IPaymentRequest paymentRequest)
        {
            var tasks = new List<Task>();
            var entity = new PaymentRequestEntity(paymentRequest);
            tasks.Add(_storage.InsertOrMergeAsync(entity));

            if (!string.IsNullOrEmpty(paymentRequest.TransferToMarketTransactionHash))
            {
                AzureIndex indexByTransferToMarketTransactionHash =
                    IndexByTransferToMarketTransactionHash.Create(entity);
                tasks.Add(_indexByTransferToMarketTransactionHash.InsertOrReplaceAsync(
                    indexByTransferToMarketTransactionHash));
            }

            return Task.WhenAll(tasks);
        }

        public Task SetTransferringToMarketAsync(string id, string transactionHash)
        {
            AzureIndex indexByTransferToMarketTransactionHash =
                IndexByTransferToMarketTransactionHash.Create(id, transactionHash);

            return Task.WhenAll(
                _storage.MergeAsync(PaymentRequestEntity.GetPartitionKey(),
                    PaymentRequestEntity.GetRowKey(id), r =>
                    {
                        r.TransferToMarketTransactionHash = transactionHash;
                        r.SettlementStatus = SettlementStatus.TransferringToMarket;
                        return r;
                    }),
                _indexByTransferToMarketTransactionHash.InsertOrReplaceAsync(indexByTransferToMarketTransactionHash));
        }

        public Task SetTransferredToMarketAsync(string id, decimal marketAmount, decimal transactionFee)
        {
            return _storage.MergeAsync(PaymentRequestEntity.GetPartitionKey(),
                PaymentRequestEntity.GetRowKey(id), r =>
                {
                    r.MarketAmount = marketAmount;
                    r.TransferToMarketTransactionFee = transactionFee;
                    r.SettlementStatus = SettlementStatus.TransferredToMarket;
                    return r;
                });
        }

        public Task SetExchangedAsync(string id, decimal marketPrice, string marketOrderId)
        {
            return _storage.MergeAsync(PaymentRequestEntity.GetPartitionKey(),
                PaymentRequestEntity.GetRowKey(id), r =>
                {
                    r.MarketOrderId = marketOrderId;
                    r.MarketPrice = marketPrice;
                    r.SettlementStatus = SettlementStatus.Exchanged;
                    return r;
                });
        }

        public Task SetTransferredToMerchantAsync(string id)
        {
            return _storage.MergeAsync(PaymentRequestEntity.GetPartitionKey(),
                PaymentRequestEntity.GetRowKey(id), r =>
                {
                    r.SettlementStatus = SettlementStatus.TransferredToMerchant;
                    return r;
                });
        }

        public Task UpdateAsync(IPaymentRequest paymentRequest)
        {
            return _storage.ReplaceAsync(new PaymentRequestEntity(paymentRequest){ETag = "*"});
        }
    }
}
