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

            var settings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = _settings.ConnectionString,
                QueueName = _settings.QueueName,
                ExchangeName = _settings.ExchangeName,
                IsDurable = true
            };

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
            var paymentRequestDetailsEvent = _mapper.Map<PaymentRequestDetailsEvent>(message);
            _eventPublisher.PublishEvent(paymentRequestDetailsEvent);

            return Task.CompletedTask;
        }
    }
}
