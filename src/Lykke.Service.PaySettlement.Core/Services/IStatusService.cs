using System.Threading.Tasks;
using Lykke.Service.PaySettlement.Core.Domain;

namespace Lykke.Service.PaySettlement.Core.Services
{
    public interface IStatusService
    {
        Task<IPaymentRequest> SetTransferToMarketQueuedAsync(string id);
        Task<IPaymentRequest> SetTransferringToMarketAsync(string id, string transactionHash);

        Task<IPaymentRequest> SetTransferredToMarketAsync(string id, decimal marketAmount,
            decimal transactionFee);

        Task<IPaymentRequest> SetExchangedAsync(string id, decimal marketPrice, 
            string marketOrderId);

        Task<IPaymentRequest> SetTransferredToMerchantAsync(string id, decimal transferredAmount);
        Task<IPaymentRequest> SetErrorAsync(string id, string errorDescription);
    }
}
