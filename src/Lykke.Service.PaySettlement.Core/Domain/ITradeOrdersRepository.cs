using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Core.Domain
{
    public interface ITradeOrdersRepository
    {
        Task InsertOrMergeTradeOrderAsync(ITradeOrder tradeOrder);
        Task<IEnumerable<ITradeOrder>> GetAsync();
        Task<IEnumerable<ITradeOrder>> GetAsync(string assetPair);
        Task DeleteAsync(ITradeOrder tradeOrder);
        Task DeleteAsync(IEnumerable<ITradeOrder> tradeOrders);
    }
}
