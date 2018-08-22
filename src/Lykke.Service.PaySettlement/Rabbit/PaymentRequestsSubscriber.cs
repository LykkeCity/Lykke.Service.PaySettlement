using Autofac;
using AutoMapper;
using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.PayInternal.Contract.PaymentRequest;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Settings;
using System;
using System.Threading.Tasks;
using Lykke.Service.PaySettlement.Core.Services;

namespace Lykke.Service.PaySettlement.Rabbit
{
    public class PaymentRequestsSubscriber : IStartable, IStopable
    {
        private readonly RabbitMqSubscriberSettings _settings;
        private readonly ILogFactory _logFactory;
        private readonly ILog _log;
        private readonly ITransferToMarketService _transferToMarketService;
        private RabbitMqSubscriber<PaymentRequestDetailsMessage> _subscriber;

        private readonly IMapper _mapper;

        public PaymentRequestsSubscriber(ITransferToMarketService transferToMarketService, 
            RabbitMqSubscriberSettings settings, ILogFactory logFactory, IMapper mapper)
        {
            _transferToMarketService = transferToMarketService;
            _logFactory = logFactory;
            _log = logFactory.CreateLog(this);
            _settings = settings;            
            _mapper = mapper;
        }

        public void Start()
        {
            var settings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = _settings.ConnectionString,
                QueueName = _settings.QueueName,
                ExchangeName = _settings.ExchangeName,
                IsDurable = false
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

        public void Stop()
        {
            _subscriber?.Stop();

            _log.Info($"<< {nameof(PaymentRequestsSubscriber)} is stopped.");
        }

        public void Dispose()
        {
            _subscriber?.Dispose();
        }

        private Task ProcessMessageAsync(PaymentRequestDetailsMessage message)
        {
            var paymentRequest = _mapper.Map<PaymentRequest>(message);
            return _transferToMarketService.AddToQueueIfSettlement(paymentRequest);
        }
    }
}
