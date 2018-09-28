using System;
using System.Threading.Tasks;
using Lykke.Cqrs;
using Lykke.Service.PaySettlement.Contracts.Commands;
using Lykke.Service.PaySettlement.Models.Exceptions;

namespace Lykke.Service.PaySettlement.Cqrs.Helpers
{
    public interface IErrorProcessHelper
    {
        Task ProcessUnknownErrorAsync(IPaymentRequestCommand command, IEventPublisher publisher,
            Exception exception, string message = "Unknown error has occured.");

        Task ProcessErrorAsync(SettlementException exception, IEventPublisher publisher);
    }
}
