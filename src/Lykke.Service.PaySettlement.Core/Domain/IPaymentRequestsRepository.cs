using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Core.Domain
{
    public interface IPaymentRequestsRepository
    {
        Task<IPaymentRequest> GetAsync(string id);
        Task<IPaymentRequest> GetByWalletAddressAsync(string walletAddress);
        Task<IEnumerable<IPaymentRequest>> GetByTransferToMarketTransactionId(string transactionId);
        Task InsertOrMergeAsync(IPaymentRequest paymentRequestEntity);
        Task SetTransferringToMarketAsync(string id, string transactionId);
        Task SetExchangedAsync(string id, decimal marketPrice, string marketOrderId);
        Task SetTransferredToMerchantAsync(string id);
        Task UpdateAsync(IPaymentRequest paymentRequest);
    }
}
