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

        internal static string GenerateRowKey(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            return id;
        }

        internal static AzureIndex Create(PaymentRequestEntity entity)
        {
            return AzureIndex.Create(GeneratePartitionKey(entity.TransferToMarketTransactionHash),
                GenerateRowKey(entity.Id), entity);
        }

        internal static AzureIndex Create(string id, string transferToMarketTransactionHash)
        {
            return AzureIndex.Create(GeneratePartitionKey(transferToMarketTransactionHash), GenerateRowKey(id),
                PaymentRequestEntity.GetPartitionKey(), PaymentRequestEntity.GetRowKey(id));
        }
    }
}
