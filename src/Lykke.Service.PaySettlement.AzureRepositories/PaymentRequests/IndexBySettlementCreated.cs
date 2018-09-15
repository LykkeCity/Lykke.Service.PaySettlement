using AzureStorage.Tables.Templates.Index;
using System;

namespace Lykke.Service.PaySettlement.AzureRepositories.PaymentRequests
{
    internal static class IndexBySettlementCreated
    {
        internal static string GeneratePartitionKey(DateTime settlementCreatedUtc)
        {
            return settlementCreatedUtc.ToString("yyyy-MM-ddTHH:mm");
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
            return AzureIndex.Create(GeneratePartitionKey(entity.SettlementCreatedUtc),
                GenerateRowKey(entity.PaymentRequestId), entity);
        }

        internal static AzureIndex Create(string merchantId, string paymentRequestId,
            DateTime settlementCreatedUtc)
        {
            return AzureIndex.Create(GeneratePartitionKey(settlementCreatedUtc),
                GenerateRowKey(paymentRequestId), PaymentRequestEntity.GetPartitionKey(merchantId),
                PaymentRequestEntity.GetRowKey(paymentRequestId));
        }
    }
}
