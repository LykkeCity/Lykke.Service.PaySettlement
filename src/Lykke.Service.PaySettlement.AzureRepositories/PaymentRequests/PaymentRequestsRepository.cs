using System.Collections.Generic;
using System.Linq;
using AzureStorage;
using AzureStorage.Tables.Templates.Index;
using Lykke.Service.PaySettlement.Core.Domain;
using System.Threading.Tasks;
using Lykke.Service.PaySettlement.Core.Repositories;

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

        public Task InsertOrReplaceAsync(IPaymentRequest paymentRequest)
        {
            var tasks = new List<Task>();
            var entity = new PaymentRequestEntity(paymentRequest);
            tasks.Add(_storage.InsertOrReplaceAsync(entity));

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
                    r.ErrorDescription = null;
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
                    r.ErrorDescription = null;
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
                    r.ErrorDescription = null;
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
                    r.ErrorDescription = null;
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
                    r.ErrorDescription = null;
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
