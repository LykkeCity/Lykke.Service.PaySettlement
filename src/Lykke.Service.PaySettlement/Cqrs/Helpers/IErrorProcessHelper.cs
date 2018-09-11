using System;
using System.Threading.Tasks;
using Lykke.Cqrs;
using Lykke.Service.PaySettlement.Contracts.Commands;

namespace Lykke.Service.PaySettlement.Cqrs.Helpers
{
    public interface IErrorProcessHelper
    {
        Task ProcessErrorAsync(IPaymentRequestCommand command, IEventPublisher publisher,
            bool setPaymentRequestStatus, Exception exception);

        Task ProcessErrorAsync(string merchantId, string paymentRequestId, IEventPublisher publisher,
            bool setPaymentRequestStatus, Exception exception);

        Task ProcessErrorAsync(IPaymentRequestCommand command, IEventPublisher publisher,
            bool setPaymentRequestStatus, string errorMesssage, Exception exception = null);

        Task ProcessErrorAsync(string merchantId, string paymentRequestId, IEventPublisher publisher,
            bool setPaymentRequestStatus, string errorMesssage, Exception exception = null);
    }
}
