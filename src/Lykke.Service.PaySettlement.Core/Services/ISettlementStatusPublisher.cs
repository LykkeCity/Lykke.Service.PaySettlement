using Lykke.Service.PaySettlement.Core.Domain;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Core.Services
{
    public interface ISettlementStatusPublisher
    {
        Task PublishAsync(ISettlementStatusChangedEvent settlementStatusChangedEvent);
        Task PublishAsync(IPaymentRequest paymentRequest);
    }
}
