﻿using Common.Log;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Service.PaySettlement.Contracts.Commands;
using Lykke.Service.PaySettlement.Contracts.Events;
using Lykke.Service.PaySettlement.Core.Services;
using System;
using System.Threading.Tasks;
using AutoMapper;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Core.Exceptions;

namespace Lykke.Service.PaySettlement.Cqrs.Helpers
{
    public class ErrorProcessHelper : IErrorProcessHelper
    {
        private readonly IPaymentRequestService _paymentRequestService;
        private readonly IMapper _mapper;
        private readonly ILog _log;

        public ErrorProcessHelper(IPaymentRequestService paymentRequestService, IMapper mapper, 
            ILogFactory logFactory)
        {
            _paymentRequestService = paymentRequestService;
            _mapper = mapper;
            _log = logFactory.CreateLog(this);
        }

        public Task ProcessUnknownErrorAsync(IPaymentRequestCommand command, IEventPublisher publisher,
            Exception exception, string message = "Unknown error has occured.")
        {
            return ProcessErrorAsync(new SettlementException(command.MerchantId, command.PaymentRequestId, 
                    SettlementProcessingError.Unknown, message, exception), publisher);
        }

        public async Task ProcessErrorAsync(SettlementException exception, IEventPublisher publisher)
        {
            _log.Error(exception.InnerException, exception.Message, new
            {
                exception.MerchantId,
                exception.PaymentRequestId
            });

            await _paymentRequestService.SetErrorAsync(exception.MerchantId,
                exception.PaymentRequestId, exception.Error, exception.Message);

            publisher.PublishEvent(new SettlementErrorEvent
            {
                PaymentRequestId = exception.PaymentRequestId,
                MerchantId = exception.MerchantId,
                Error = _mapper.Map<Contracts.SettlementProcessingError>(exception.Error),
                ErrorDescription = exception.Message
            });
        }
    }
}
