using System;
using System.Collections.Generic;
using System.Text;
using AzureStorage.Tables.Templates.Index;

namespace Lykke.Service.PaySettlement.AzureRepositories.PaymentRequests
{
    internal static class IndexByTransferToMarketTransactionId
    {
        internal static string GeneratePartitionKey(string transferToMarketTransactionId)
        {
            if (string.IsNullOrEmpty(transferToMarketTransactionId))
            {
                throw new ArgumentNullException(nameof(transferToMarketTransactionId));
            }

            return transferToMarketTransactionId;
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
            return AzureIndex.Create(GeneratePartitionKey(entity.TransferToMarketTransactionId),
                GenerateRowKey(entity.Id), entity);
        }

        internal static AzureIndex Create(string id, string transferToMarketTransactionId)
        {
            return AzureIndex.Create(GeneratePartitionKey(transferToMarketTransactionId), GenerateRowKey(id),
                PaymentRequestEntity.GetPartitionKey(), PaymentRequestEntity.GetRowKey(id));
        }
    }
}
