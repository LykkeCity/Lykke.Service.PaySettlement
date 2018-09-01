using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Core.Services
{
    public interface ITradeService
    {
        Task AddToQueueIfTransferredAsync(string transactionHash, decimal fee);
    }
}
