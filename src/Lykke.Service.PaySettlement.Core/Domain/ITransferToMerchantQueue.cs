using System;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Core.Domain
{
    public interface ITransferToMerchantQueue
    {
        Task AddAsync(TransferToMerchantMessage transferToMerchantMessage);
        Task<bool> ProcessTransferAsync(Func<TransferToMerchantMessage, Task<bool>> processor);
    }
}
