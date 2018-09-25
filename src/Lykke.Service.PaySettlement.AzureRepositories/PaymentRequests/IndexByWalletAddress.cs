using AzureStorage.Tables.Templates.Index;
using System;

namespace Lykke.Service.PaySettlement.AzureRepositories.PaymentRequests
{
    internal static class IndexByWalletAddress
    {
        internal static string GeneratePartitionKey(string walletAddress)
        {
            if (string.IsNullOrEmpty(walletAddress))
            {
                throw new ArgumentNullException(nameof(walletAddress));
            }

            return walletAddress;
        }

        internal static string GenerateRowKey()
        {
            return "WalletAddressIndex";
        }

        internal static AzureIndex Create(PaymentRequestEntity entity)
        {
            return AzureIndex.Create(GeneratePartitionKey(entity.WalletAddress),
                GenerateRowKey(), entity);
        }

        internal static AzureIndex Create(string merchantId, string paymentRequestId, 
            string walletAddress)
        {
            return AzureIndex.Create(GeneratePartitionKey(walletAddress), 
                GenerateRowKey(), PaymentRequestEntity.GetPartitionKey(merchantId), 
                PaymentRequestEntity.GetRowKey(paymentRequestId));
        }
    }
}
