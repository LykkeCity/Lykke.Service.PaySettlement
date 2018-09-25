using System;
using System.Threading.Tasks;
using Lykke.Service.PaySettlement.Core.Domain;

namespace Lykke.Service.PaySettlement.Core.Repositories
{
    public interface ITransferToMarketQueue
    {
        Task AddPaymentRequestsAsync(IPaymentRequest paymentRequest);

        Task<T> ProcessTransferAsync<T>(Func<TransferToMarketMessage[], Task<T>> processor,
            int maxCount = int.MaxValue) where T : IMessageProcessorResult;
    }
}
