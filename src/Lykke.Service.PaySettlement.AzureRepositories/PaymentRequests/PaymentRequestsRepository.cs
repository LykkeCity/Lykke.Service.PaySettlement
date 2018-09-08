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
        private readonly INoSQLTableStorage<AzureIndex> _indexByError;

        public PaymentRequestsRepository(
            INoSQLTableStorage<PaymentRequestEntity> storage,
            INoSQLTableStorage<AzureIndex> indexByTransferToMarketTransactionHash,
            INoSQLTableStorage<AzureIndex> indexByError)
        {
            _storage = storage;
            _indexByTransferToMarketTransactionHash = indexByTransferToMarketTransactionHash;
            _indexByError = indexByError;
        }

        public async Task<IPaymentRequest> GetAsync(string merchantId, string id)
        {
            return await _storage.GetDataAsync(PaymentRequestEntity.GetPartitionKey(merchantId),
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

        public async Task<IPaymentRequest> SetTransferToMarketQueuedAsync(string merchantId, string id)
        {
            return await _storage.MergeAsync(PaymentRequestEntity.GetPartitionKey(merchantId),
                PaymentRequestEntity.GetRowKey(id), r =>
                {
                    r.SettlementStatus = SettlementStatus.TransferToMarketQueued;
                    return r;
                });
        }

        public async Task<IPaymentRequest> SetTransferringToMarketAsync(string merchantId, string id, string transactionHash)
        {
            AzureIndex indexByTransferToMarketTransactionHash =
                IndexByTransferToMarketTransactionHash.Create(merchantId, id, transactionHash);

            var mergeTask = _storage.MergeAsync(PaymentRequestEntity.GetPartitionKey(merchantId),
                PaymentRequestEntity.GetRowKey(id), r =>
                {
                    r.TransferToMarketTransactionHash = transactionHash;
                    r.SettlementStatus = SettlementStatus.TransferringToMarket;
                    return r;
                });

            await Task.WhenAll(mergeTask,
                _indexByTransferToMarketTransactionHash.InsertOrReplaceAsync(
                    indexByTransferToMarketTransactionHash));

            return mergeTask.Result;
        }

        public async Task<IPaymentRequest> SetTransferredToMarketAsync(string merchantId, string id, 
            decimal marketAmount, decimal transactionFee)
        {            
            return await _storage.MergeAsync(PaymentRequestEntity.GetPartitionKey(merchantId),
                PaymentRequestEntity.GetRowKey(id), r =>
                {
                    r.MarketAmount = marketAmount;
                    r.TransferToMarketTransactionFee = transactionFee;
                    r.SettlementStatus = SettlementStatus.TransferredToMarket;
                    return r;
                });
        }

        public async Task<IPaymentRequest> SetExchangedAsync(string merchantId, string id, 
            decimal marketPrice, string marketOrderId)
        {
            return await _storage.MergeAsync(PaymentRequestEntity.GetPartitionKey(merchantId),
                PaymentRequestEntity.GetRowKey(id), r =>
                {
                    r.MarketOrderId = marketOrderId;
                    r.MarketPrice = marketPrice;
                    r.SettlementStatus = SettlementStatus.Exchanged;
                    return r;
                });
        }

        public async Task<IPaymentRequest> SetTransferredToMerchantAsync(string merchantId, string id, 
            decimal transferredAmount)
        {
            return await _storage.MergeAsync(PaymentRequestEntity.GetPartitionKey(merchantId),
                PaymentRequestEntity.GetRowKey(id), r =>
                {
                    r.SettlementStatus = SettlementStatus.TransferredToMerchant;
                    r.TransferredAmount = transferredAmount;
                    return r;
                });
        }

        public async Task<IPaymentRequest> SetErrorAsync(string merchantId, string id, 
            string errorDescription = null)
        {
            AzureIndex indexByError = IndexByError.Create(merchantId, id);

            var mergeTask = _storage.MergeAsync(PaymentRequestEntity.GetPartitionKey(merchantId),
                PaymentRequestEntity.GetRowKey(id), r =>
                {
                    r.Error = true;
                    r.ErrorDescription = errorDescription;
                    return r;
                });

            await Task.WhenAll(mergeTask,
                _indexByError.InsertOrReplaceAsync(indexByError));

            return mergeTask.Result;
        }
    }
}
