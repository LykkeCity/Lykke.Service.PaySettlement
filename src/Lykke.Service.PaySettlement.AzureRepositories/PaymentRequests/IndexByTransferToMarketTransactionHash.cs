using AzureStorage.Tables.Templates.Index;
using System;

namespace Lykke.Service.PaySettlement.AzureRepositories.PaymentRequests
{
    internal static class IndexByTransferToMarketTransactionHash
    {
        internal static string GeneratePartitionKey(string transferToMarketTransactionHash)
        {
            if (string.IsNullOrEmpty(transferToMarketTransactionHash))
            {
                throw new ArgumentNullException(nameof(transferToMarketTransactionHash));
            }

            return transferToMarketTransactionHash;
        }

        internal static string GenerateRowKey(string paymentRequestId)
        {
            if (string.IsNullOrEmpty(paymentRequestId))
            {
                throw new ArgumentNullException(nameof(paymentRequestId));
            }

            return paymentRequestId;
        }

        internal static AzureIndex Create(PaymentRequestEntity entity)
        {
            return AzureIndex.Create(GeneratePartitionKey(entity.TransferToMarketTransactionHash),
                GenerateRowKey(entity.PaymentRequestId), entity);
        }

        internal static AzureIndex Create(string merchantId, string paymentRequestId, 
            string transferToMarketTransactionHash)
        {
            return AzureIndex.Create(GeneratePartitionKey(transferToMarketTransactionHash), 
                GenerateRowKey(paymentRequestId), PaymentRequestEntity.GetPartitionKey(merchantId), 
                PaymentRequestEntity.GetRowKey(paymentRequestId));
        }
    }
}
