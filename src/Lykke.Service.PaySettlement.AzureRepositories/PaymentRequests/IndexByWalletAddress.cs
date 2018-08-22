using AzureStorage.Tables.Templates.Index;
using System;

namespace Lykke.Service.PaySettlement.AzureRepositories.PaymentRequests
{
    internal static class IndexByWalletAddress
    {
        internal static string GeneratePartitionKey()
        {
            return "IndexByWalletAddress";
        }

        internal static string GenerateRowKey(string walletAddress)
        {
            if (string.IsNullOrEmpty(walletAddress))
            {
                throw new ArgumentNullException(nameof(walletAddress));
            }

            return walletAddress;
        }

        internal static AzureIndex Create(PaymentRequestEntity entity)
        {
            return AzureIndex.Create(GeneratePartitionKey(), GenerateRowKey(entity.WalletAddress), entity);
        }
    }
}
