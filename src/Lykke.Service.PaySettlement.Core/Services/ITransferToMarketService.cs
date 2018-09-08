using System.Threading.Tasks;
using Lykke.Service.PaySettlement.Core.Domain;

namespace Lykke.Service.PaySettlement.Core.Services
{
    public interface ITransferToMarketService
    {
        Task AddToQueueIfSettlementAsync(IPaymentRequest paymentRequest);
    }
}
