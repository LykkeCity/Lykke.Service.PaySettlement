using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Core.Domain
{
    public interface IPaymentRequestsRepository
    {
        Task<IPaymentRequest> GetAsync(string id);
        Task<IEnumerable<IPaymentRequest>> GetByTransferToMarketTransactionHash(string transactionHash);
        Task InsertOrMergeAsync(IPaymentRequest paymentRequestEntity);
        Task SetTransferringToMarketAsync(string id, string transactionHash);
        Task SetTransferredToMarketAsync(string id, decimal marketAmount, decimal transactionFee);
        Task SetExchangedAsync(string id, decimal marketPrice, string marketOrderId);
        Task SetTransferredToMerchantAsync(string id);
        Task UpdateAsync(IPaymentRequest paymentRequest);
    }
}
