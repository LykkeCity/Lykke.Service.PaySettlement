using System.Threading.Tasks;
using Lykke.Service.PaySettlement.Core.Domain;

namespace Lykke.Service.PaySettlement.Core.Services
{
    public interface IExchangeService
    {
        Task<IExchangeOrder> AddToQueueAsync(IPaymentRequest paymentRequest,
            decimal transferredAmount);

        Task<ExchangeResult> ExchangeAsync();
    }
}
