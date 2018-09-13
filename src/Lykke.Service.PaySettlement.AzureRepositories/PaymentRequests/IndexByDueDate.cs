﻿using AzureStorage.Tables.Templates.Index;
using System;

namespace Lykke.Service.PaySettlement.AzureRepositories.PaymentRequests
{
    internal static class IndexByDueDate
    {
        internal static string GeneratePartitionKey(DateTime dueDate)
        {
            return dueDate.ToString("yyyy-MM-ddTHH:mm");
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
            return AzureIndex.Create(GeneratePartitionKey(entity.DueDate),
                GenerateRowKey(entity.PaymentRequestId), entity);
        }

        internal static AzureIndex Create(string merchantId, string paymentRequestId,
            DateTime dueDate)
        {
            return AzureIndex.Create(GeneratePartitionKey(dueDate),
                GenerateRowKey(paymentRequestId), PaymentRequestEntity.GetPartitionKey(merchantId),
                PaymentRequestEntity.GetRowKey(paymentRequestId));
        }
    }
}
