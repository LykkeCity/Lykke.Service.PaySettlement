using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Service.PaySettlement.Contracts.Events;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Core.Services;
using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Service.PaySettlement.Cqrs.Helpers;
using Lykke.Service.PaySettlement.Models.Exceptions;

namespace Lykke.Service.PaySettlement.Cqrs.Processes
{
    [UsedImplicitly]
    public class ExchangeProcess : TimerPeriod, IProcess
    {
        private readonly IExchangeService _exchangeService;
        private readonly IPaymentRequestService _paymentRequestService;
        private readonly IErrorProcessHelper _errorProcessHelper;
        private IEventPublisher _eventPublisher;        
        private readonly ILog _log;

        public ExchangeProcess(IExchangeService exchangeService, 
            IPaymentRequestService paymentRequestService, TimeSpan interval,
            IErrorProcessHelper errorProcessHelper, ILogFactory logFactory) : base(interval, logFactory)
        {
            _exchangeService = exchangeService;
            _paymentRequestService = paymentRequestService;
            _errorProcessHelper = errorProcessHelper;
            _log = logFactory.CreateLog(this);
        }

        [UsedImplicitly]
        public void Start(ICommandSender commandSender, IEventPublisher eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }

        public override async Task Execute()
        {
            if (_eventPublisher == null)
            {
                return;
            }

            ExchangeResult result;
            do
            {
                result = await _exchangeService.ExchangeAsync();

                await ProcessExchangeResultAsync(result);

            } while (result != null);
        }

        private async Task ProcessExchangeResultAsync(ExchangeResult result)
        {
            if (result == null)
            {
                return;
            }

            if (result.Error == SettlementProcessingError.None)
            {
                await _paymentRequestService.SetExchangedAsync(result.MerchantId, 
                    result.PaymentRequestId, result.MarketPrice, result.MarketOrderId);

                _eventPublisher.PublishEvent(new SettlementExchangedEvent
                {
                    PaymentRequestId = result.PaymentRequestId,
                    MerchantId = result.MerchantId,
                    MarketPrice = result.MarketPrice,
                    MarketOrderId = result.MarketOrderId,
                    AssetPairId = result.AssetPairId
                });
            }
            else
            {
                var settlementException = new SettlementException(result.MerchantId, result.PaymentRequestId,
                    result.Error, result.ErrorMessage, result.Exception);
                await _errorProcessHelper.ProcessErrorAsync(settlementException, _eventPublisher, true);
            }
        }
    }
}
