using System;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Core.Domain
{
    public interface ITransferToMarketQueue
    {
        Task AddPaymentRequestsAsync(IPaymentRequest paymentRequest);

        Task<int> ProcessTransferAsync(Func<TransferToMarketMessage[], Task<bool>> processor,
            int maxCount = int.MaxValue);
    }
}
