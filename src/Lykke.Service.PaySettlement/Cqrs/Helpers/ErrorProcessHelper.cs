using Common.Log;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Service.PaySettlement.Contracts.Commands;
using Lykke.Service.PaySettlement.Contracts.Events;
using Lykke.Service.PaySettlement.Core.Services;
using Lykke.Service.PaySettlement.Models.Exceptions;
using System;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Cqrs.Helpers
{
    public class ErrorProcessHelper : IErrorProcessHelper
    {
        private readonly IPaymentRequestService _paymentRequestService;
        private readonly ILog _log;

        public ErrorProcessHelper(IPaymentRequestService paymentRequestService, ILogFactory logFactory)
        {
            _paymentRequestService = paymentRequestService;
            _log = logFactory.CreateLog(this);
        }

        public Task ProcessErrorAsync(IPaymentRequestCommand command, IEventPublisher publisher,
            bool setPaymentRequestStatus, Exception exception)
        {
            return ProcessErrorAsync(command.MerchantId, command.PaymentRequestId, publisher,
                setPaymentRequestStatus, exception);
        }

        public Task ProcessErrorAsync(string merchantId, string paymentRequestId, IEventPublisher publisher,
            bool setPaymentRequestStatus, Exception exception)
        {
            var exceptionForLog = exception;
            string errorMesssage = "Unknown error has occured.";
            if (exception is SettlementException settlementException)
            {
                errorMesssage = settlementException.Message;
                exceptionForLog = exception.InnerException;
            }

            return ProcessErrorAsync(merchantId, paymentRequestId, publisher, setPaymentRequestStatus, errorMesssage, exceptionForLog);
        }

        public Task ProcessErrorAsync(IPaymentRequestCommand command, IEventPublisher publisher,
            bool setPaymentRequestStatus, string errorMesssage, Exception exception = null)
        {
            return ProcessErrorAsync(command.MerchantId, command.PaymentRequestId, publisher,
                setPaymentRequestStatus, errorMesssage, exception);
        }

        public async Task ProcessErrorAsync(string merchantId, string paymentRequestId, IEventPublisher publisher,
            bool setPaymentRequestStatus, string errorMesssage, Exception exception = null)
        {
            _log.Error(exception, errorMesssage, new
            {
                MerchantId = merchantId,
                PaymentRequestId = paymentRequestId
            });

            if (setPaymentRequestStatus)
            {
                await _paymentRequestService.SetErrorAsync(merchantId, paymentRequestId, errorMesssage);
            }

            publisher.PublishEvent(new SettlementErrorEvent
            {
                PaymentRequestId = paymentRequestId,
                MerchantId = merchantId,
                ErrorDescription = errorMesssage
            });
        }
    }
}
