using System;
using AzureStorage;
using AzureStorage.Tables.Templates.Index;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Core.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.PaySettlement.AzureRepositories.PaymentRequests
{
    public class PaymentRequestsRepository : IPaymentRequestsRepository
    {
        private readonly INoSQLTableStorage<PaymentRequestEntity> _storage;
        private readonly INoSQLTableStorage<AzureIndex> _indexByTransferToMarketTransactionHash;
        private readonly INoSQLTableStorage<AzureIndex> _indexByWalletAddress;
        private readonly INoSQLTableStorage<AzureIndex> _indexBySettlementCreated;

        public PaymentRequestsRepository(
            INoSQLTableStorage<PaymentRequestEntity> storage,
            INoSQLTableStorage<AzureIndex> indexByTransferToMarketTransactionHash,
            INoSQLTableStorage<AzureIndex> indexByWalletAddress,
            INoSQLTableStorage<AzureIndex> indexBySettlementCreated)
        {
            _storage = storage;
            _indexByTransferToMarketTransactionHash = indexByTransferToMarketTransactionHash;
            _indexByWalletAddress = indexByWalletAddress;
            _indexBySettlementCreated = indexBySettlementCreated;
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

        public async Task<IPaymentRequest> GetByWalletAddressAsync(string walletAddress)
        {
            var index = await _indexByWalletAddress.GetDataAsync(
                IndexByWalletAddress.GeneratePartitionKey(walletAddress), IndexByWalletAddress.GenerateRowKey());

            return await _storage.GetDataAsync(index);
        }

        public async Task<(IEnumerable<IPaymentRequest> Entities, string ContinuationToken)> GetBySettlementCreatedAsync(
            DateTime from, DateTime to, int take, string continuationToken = null)
        {
            string geDate = IndexBySettlementCreated.GeneratePartitionKey(from);

            string leDate = IndexBySettlementCreated.GeneratePartitionKey(to);

            var filter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, geDate),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThanOrEqual, leDate));

            var query = new TableQuery<AzureIndex>().Where(filter);

            var indices = await _indexBySettlementCreated.GetDataWithContinuationTokenAsync(query, take, 
                continuationToken);
            var paymentRequests = await _storage.GetDataAsync(indices.Entities);

            (IEnumerable<IPaymentRequest> Entities, string ContinuationToken) result = 
                (paymentRequests, indices.ContinuationToken);

            return result;
        }

        public async Task<(IEnumerable<IPaymentRequest> Entities, string ContinuationToken)> GetByMerchantAsync(
            string merchantId, int take, string continuationToken = null)
        {
            var paymentRequests = await _storage.GetDataWithContinuationTokenAsync(merchantId, take, continuationToken);
            
            (IEnumerable<IPaymentRequest> Entities, string ContinuationToken) result =
                (paymentRequests.Entities, paymentRequests.ContinuationToken);

            return result;
        }

        public Task InsertOrReplaceAsync(IPaymentRequest paymentRequest)
        {
            var tasks = new List<Task>();
            var entity = new PaymentRequestEntity(paymentRequest);
            entity.SettlementCreatedUtc = DateTime.UtcNow;

            tasks.Add(_storage.InsertOrReplaceAsync(entity));

            AzureIndex indexByWalletAddress = IndexByWalletAddress.Create(entity);
            tasks.Add(_indexByWalletAddress.InsertOrReplaceAsync(indexByWalletAddress));

            AzureIndex indexBySettlementCreated = IndexBySettlementCreated.Create(entity);
            tasks.Add(_indexBySettlementCreated.InsertOrReplaceAsync(indexBySettlementCreated));

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
                    r.Error = SettlementProcessingError.None;
                    r.ErrorDescription = string.Empty;
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
                    r.Error = SettlementProcessingError.None;
                    r.ErrorDescription = string.Empty;
                    return r;
                });

            await Task.WhenAll(mergeTask,
                _indexByTransferToMarketTransactionHash.InsertOrReplaceAsync(
                    indexByTransferToMarketTransactionHash));

            return mergeTask.Result;
        }

        public async Task<IPaymentRequest> SetExchangeQueuedAsync(string merchantId, string id, 
            decimal exchangeAmount, decimal transactionFee)
        {            
            return await _storage.MergeAsync(PaymentRequestEntity.GetPartitionKey(merchantId),
                PaymentRequestEntity.GetRowKey(id), r =>
                {
                    r.ExchangeAmount = exchangeAmount;
                    r.TransferToMarketTransactionFee = transactionFee;
                    r.TransferedToMarketUtc = DateTime.UtcNow;
                    r.SettlementStatus = SettlementStatus.ExchangeQueued;
                    r.Error = SettlementProcessingError.None;
                    r.ErrorDescription = string.Empty;
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
                    r.ExchangedUtc = DateTime.UtcNow;
                    r.SettlementStatus = SettlementStatus.Exchanged;
                    r.Error = SettlementProcessingError.None;
                    r.ErrorDescription = string.Empty;
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
                    r.TransferedToMerchantUtc = DateTime.UtcNow;
                    r.Error = SettlementProcessingError.None;
                    r.ErrorDescription = string.Empty;
                    return r;
                });
        }

        public async Task<IPaymentRequest> SetErrorAsync(string merchantId, string id,
            SettlementProcessingError error, string errorDescription = null)
        {
            return await _storage.MergeAsync(PaymentRequestEntity.GetPartitionKey(merchantId),
                PaymentRequestEntity.GetRowKey(id), r =>
                {
                    r.Error = error;
                    r.ErrorDescription = errorDescription;
                    return r;
                });
        }
    }
}
