using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Core.Services
{
    public interface ITradeService
    {
        Task AddToQueueIfTransferred(string transactionHash, decimal fee);
    }
}
