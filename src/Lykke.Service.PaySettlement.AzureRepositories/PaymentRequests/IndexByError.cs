using AzureStorage.Tables.Templates.Index;
using System;

namespace Lykke.Service.PaySettlement.AzureRepositories.PaymentRequests
{
    internal static class IndexByError
    {
        internal static string GeneratePartitionKey()
        {
            return "error";
        }

        internal static string GenerateRowKey(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            return id;
        }

        internal static AzureIndex Create(string merchantId, string id)
        {
            return AzureIndex.Create(GeneratePartitionKey(), GenerateRowKey(id),
                PaymentRequestEntity.GetPartitionKey(merchantId), PaymentRequestEntity.GetRowKey(id));
        }
    }
}
