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
        private readonly INoSQLTableStorage<AzureIndex> _indexByDueDate;

        public PaymentRequestsRepository(
            INoSQLTableStorage<PaymentRequestEntity> storage,
            INoSQLTableStorage<AzureIndex> indexByTransferToMarketTransactionHash,
            INoSQLTableStorage<AzureIndex> indexByWalletAddress,
            INoSQLTableStorage<AzureIndex> indexByDueDate)
        {
            _storage = storage;
            _indexByTransferToMarketTransactionHash = indexByTransferToMarketTransactionHash;
            _indexByWalletAddress = indexByWalletAddress;
            _indexByDueDate = indexByDueDate;
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

            if (index == null)
            {
                return null;
            }

            return await _storage.GetDataAsync(index);
        }

        public async Task<IPaymentRequest> GetByWalletAddressAsync(string walletAddress)
        {
            var index = await _indexByWalletAddress.GetDataAsync(
                IndexByWalletAddress.GeneratePartitionKey(walletAddress), IndexByWalletAddress.GenerateRowKey());

            if (index == null)
            {
                return null;
            }

            return await _storage.GetDataAsync(index);
        }

        public async Task<(IEnumerable<IPaymentRequest> Entities, string ContinuationToken)> GetByDueDateAsync(
            DateTime fromDueDate, DateTime toDueDate, int take, string continuationToken = null)
        {
            string geDate = IndexByDueDate.GeneratePartitionKey(fromDueDate);

            string leDate = IndexByDueDate.GeneratePartitionKey(toDueDate);

            var filter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, geDate),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThanOrEqual, leDate));

            var query = new TableQuery<AzureIndex>().Where(filter);

            var indices = await _indexByDueDate.GetDataWithContinuationTokenAsync(query, take, continuationToken);
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
            tasks.Add(_storage.InsertOrReplaceAsync(entity));

            AzureIndex indexByWalletAddress = IndexByWalletAddress.Create(entity);
            tasks.Add(_indexByWalletAddress.InsertOrReplaceAsync(indexByWalletAddress));

            AzureIndex indexByDueDate = IndexByDueDate.Create(entity);
            tasks.Add(_indexByDueDate.InsertOrReplaceAsync(indexByDueDate));

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
                    r.Error = false;
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
                    r.Error = false;
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
                    r.SettlementStatus = SettlementStatus.ExchangeQueued;
                    r.Error = false;
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
                    r.SettlementStatus = SettlementStatus.Exchanged;
                    r.Error = false;
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
                    r.Error = false;
                    r.ErrorDescription = string.Empty;
                    return r;
                });
        }

        public async Task<IPaymentRequest> SetErrorAsync(string merchantId, string id, 
            string errorDescription = null)
        {
            return await _storage.MergeAsync(PaymentRequestEntity.GetPartitionKey(merchantId),
                PaymentRequestEntity.GetRowKey(id), r =>
                {
                    r.Error = true;
                    r.ErrorDescription = errorDescription;
                    return r;
                });
        }
    }
}
