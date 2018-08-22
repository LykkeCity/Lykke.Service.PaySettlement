using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Core.Services
{
    public interface ITransferToMerchantService
    {
        Task AddToQueue(string paymentRequestId);
    }
}
