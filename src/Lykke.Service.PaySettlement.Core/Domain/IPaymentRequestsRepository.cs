using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Core.Domain
{
    public interface IPaymentRequestsRepository
    {
        Task<IPaymentRequest> GetAsync(string id);
        Task<IEnumerable<IPaymentRequest>> GetByTransferToMarketTransactionHash(string transactionHash);
        Task InsertOrMergeAsync(IPaymentRequest paymentRequestEntity);
        Task<IPaymentRequest> SetTransferringToMarketAsync(string id, string transactionHash);
        Task<IPaymentRequest> SetTransferredToMarketAsync(string id, decimal marketAmount, decimal transactionFee);
        Task<IPaymentRequest> SetExchangedAsync(string id, decimal marketPrice, string marketOrderId);
        Task<IPaymentRequest> SetTransferredToMerchantAsync(string id, decimal transferredAmount);
        Task UpdateAsync(IPaymentRequest paymentRequest);
    }
}
