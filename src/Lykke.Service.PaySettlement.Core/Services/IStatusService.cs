using System.Threading.Tasks;
using Lykke.Service.PaySettlement.Core.Domain;

namespace Lykke.Service.PaySettlement.Core.Services
{
    public interface IStatusService
    {
        Task<IPaymentRequest> SetTransferToMarketQueuedAsync(string merchantId, string id);
        Task<IPaymentRequest> SetTransferringToMarketAsync(string merchantId, string id, 
            string transactionHash);

        Task<IPaymentRequest> SetTransferredToMarketAsync(ITradeOrder tradeOrder, decimal transactionFee);

        Task<IPaymentRequest> SetExchangedAsync(string merchantId, string id, decimal marketPrice, 
            string marketOrderId);

        Task<IPaymentRequest> SetTransferredToMerchantAsync(string merchantId, string id, 
            decimal transferredAmount);
        Task<IPaymentRequest> SetErrorAsync(string merchantId, string id, string errorDescription);
    }
}
