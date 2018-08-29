using AzureStorage.Tables.Templates.Index;
using System;
using Lykke.Service.PaySettlement.Core.Domain;

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

        internal static bool IsIndexable(IPaymentRequest entity)
        {
            return entity.Error;
        }

        internal static AzureIndex Create(PaymentRequestEntity entity)
        {
            return AzureIndex.Create(GeneratePartitionKey(),
                GenerateRowKey(entity.Id), entity);
        }

        internal static AzureIndex Create(string id)
        {
            return AzureIndex.Create(GeneratePartitionKey(), GenerateRowKey(id),
                PaymentRequestEntity.GetPartitionKey(), PaymentRequestEntity.GetRowKey(id));
        }
    }
}
