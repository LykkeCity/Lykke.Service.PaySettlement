using AutoMapper;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.PayInternal.Contract.PaymentRequest;
using Lykke.Service.PaySettlement.Contracts.Events;
using Lykke.Service.PaySettlement.Settings;
using System;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Rabbit
{
    public class PaymentRequestsSubscriber : IProcess
    {
        private readonly RabbitMqSubscriberSettings _settings;
        private readonly ILogFactory _logFactory;
        private readonly ILog _log;
        private RabbitMqSubscriber<PaymentRequestDetailsMessage> _subscriber;
        private IEventPublisher _eventPublisher;

        private readonly IMapper _mapper;

        public PaymentRequestsSubscriber(RabbitMqSubscriberSettings settings, 
            ILogFactory logFactory, IMapper mapper)
        {
            _logFactory = logFactory;
            _log = logFactory.CreateLog(this);
            _settings = settings;            
            _mapper = mapper;
        }

        public void Start(ICommandSender commandSender, IEventPublisher eventPublisher)
        {
            _eventPublisher = eventPublisher;

            var settings = RabbitMqSubscriptionSettings
                .ForSubscriber(
                    _settings.ConnectionString, 
                    _settings.ExchangeName, 
                    nameof(PaySettlement))
                .MakeDurable();

            _subscriber = new RabbitMqSubscriber<PaymentRequestDetailsMessage>(
                    _logFactory,
                    settings,
                    new ResilientErrorHandlingStrategy(
                        settings: settings,
                        logFactory: _logFactory,
                        retryTimeout: TimeSpan.FromSeconds(10)))
                .SetMessageDeserializer(new JsonMessageDeserializer<PaymentRequestDetailsMessage>())
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding();

            _subscriber.Start();

            _log.Info($"<< {nameof(PaymentRequestsSubscriber)} is started.");
        }

        public void Dispose()
        {
            _subscriber?.Stop();
            _log.Info($"<< {nameof(PaymentRequestsSubscriber)} is stopped.");
            _subscriber?.Dispose();
        }

        private Task ProcessMessageAsync(PaymentRequestDetailsMessage message)
        {
            if (message.Status == PaymentRequestStatus.Confirmed)
            {
                var paymentRequestConfirmedEvent = _mapper.Map<PaymentRequestConfirmedEvent>(message);
                _eventPublisher.PublishEvent(paymentRequestConfirmedEvent);
            }

            return Task.CompletedTask;
        }
    }
}
