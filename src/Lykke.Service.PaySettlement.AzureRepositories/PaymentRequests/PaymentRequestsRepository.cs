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
        private readonly INoSQLTableStorage<AzureIndex> _indexByWalletAddress;
        private readonly INoSQLTableStorage<AzureIndex> _indexByTransferToMarketTransactionId;

        public PaymentRequestsRepository(
            INoSQLTableStorage<PaymentRequestEntity> storage,
            INoSQLTableStorage<AzureIndex> indexByWalletAddress,
            INoSQLTableStorage<AzureIndex> indexByTransferToMarketTransactionId)
        {
            _storage = storage;
            _indexByWalletAddress = indexByWalletAddress;
            _indexByTransferToMarketTransactionId = indexByTransferToMarketTransactionId;
        }

        public async Task<IPaymentRequest> GetAsync(string id)
        {
            return await _storage.GetDataAsync(PaymentRequestEntity.GetPartitionKey(),
                PaymentRequestEntity.GetRowKey(id));
        }

        public async Task<IPaymentRequest> GetByWalletAddressAsync(string walletAddress)
        {
            var index = await _indexByWalletAddress.GetDataAsync(IndexByWalletAddress.GeneratePartitionKey(),
                IndexByWalletAddress.GenerateRowKey(walletAddress));

            return await _storage.GetDataAsync(index);
        }

        public async Task<IEnumerable<IPaymentRequest>> GetByTransferToMarketTransactionId(string transactionId)
        {
            var index = await _indexByTransferToMarketTransactionId.GetDataAsync(
                IndexByTransferToMarketTransactionId.GeneratePartitionKey(transactionId));
            return await _storage.GetDataAsync(index);
        }

        public Task InsertOrMergeAsync(IPaymentRequest paymentRequest)
        {
            var tasks = new List<Task>();
            var entity = new PaymentRequestEntity(paymentRequest);
            tasks.Add(_storage.InsertOrMergeAsync(entity));

            AzureIndex indexByWalletAddress = IndexByWalletAddress.Create(entity);
            tasks.Add(_indexByWalletAddress.InsertOrReplaceAsync(indexByWalletAddress));

            if (!string.IsNullOrEmpty(paymentRequest.TransferToMarketTransactionId))
            {
                AzureIndex indexByTransferToMarketTransactionId =
                    IndexByTransferToMarketTransactionId.Create(entity);
                tasks.Add(_indexByTransferToMarketTransactionId.InsertOrReplaceAsync(
                    indexByTransferToMarketTransactionId));
            }

            return Task.WhenAll(tasks);
        }

        public Task SetTransferringToMarketAsync(string id, string transactionId)
        {
            AzureIndex indexByTransferToMarketTransactionId =
                IndexByTransferToMarketTransactionId.Create(id, transactionId);

            return Task.WhenAll(
                _storage.MergeAsync(PaymentRequestEntity.GetPartitionKey(),
                    PaymentRequestEntity.GetRowKey(id), r =>
                    {
                        r.TransferToMarketTransactionId = transactionId;
                        r.SettlementStatus = SettlementStatus.TransferringToMarket;
                        return r;
                    }),
                _indexByTransferToMarketTransactionId.InsertOrReplaceAsync(indexByTransferToMarketTransactionId));
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
            return _storage.ReplaceAsync(new PaymentRequestEntity(paymentRequest));
        }
    }
}
