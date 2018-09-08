using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.PaySettlement.Core.Domain;

namespace Lykke.Service.PaySettlement.Core.Repositories
{
    public interface IPaymentRequestsRepository
    {
        Task<IPaymentRequest> GetAsync(string merchantId, string id);

        Task<IEnumerable<IPaymentRequest>> GetByTransferToMarketTransactionHash(string transactionHash);

        Task InsertOrReplaceAsync(IPaymentRequest paymentRequestEntity);

        Task<IPaymentRequest> SetTransferToMarketQueuedAsync(string merchantId, string id);

        Task<IPaymentRequest> SetTransferringToMarketAsync(string merchantId, string id, 
            string transactionHash);

        Task<IPaymentRequest> SetExchangeQueuedAsync(string merchantId, string id, 
            decimal exchangeAmount, decimal transactionFee);

        Task<IPaymentRequest> SetExchangedAsync(string merchantId, string id, decimal marketPrice, 
            string marketOrderId);

        Task<IPaymentRequest> SetTransferredToMerchantAsync(string merchantId, string id, 
            decimal transferredAmount);

        Task<IPaymentRequest> SetErrorAsync(string merchantId, string id, string errorDescription = null);
    }
}
