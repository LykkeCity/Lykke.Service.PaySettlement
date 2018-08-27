using Autofac;
using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Settings;
using System.Threading.Tasks;
using AutoMapper;
using Lykke.Service.PaySettlement.Core.Services;

namespace Lykke.Service.PaySettlement.Rabbit
{
    public class SettlementStatusPublisher: ISettlementStatusPublisher, IStartable, IStopable
    {
        private readonly RabbitMqPublisherSettings _settings;
        private readonly ILog _log;
        private readonly ILogFactory _logFactory;
        private RabbitMqPublisher<ISettlementStatusChangedEvent> _publisher;
        private readonly IMapper _mapper;

        public SettlementStatusPublisher(RabbitMqPublisherSettings settings,
            IMapper mapper, ILogFactory logFactory)
        {
            _settings = settings;
            _mapper = mapper;
            _logFactory = logFactory;
            _log = logFactory.CreateLog(this);
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings.CreateForPublisher(
                _settings.ConnectionString, _settings.ExchangeName);
            settings.MakeDurable();

            _publisher = new RabbitMqPublisher<ISettlementStatusChangedEvent>(_logFactory, settings);

            _publisher.DisableInMemoryQueuePersistence()
                .PublishSynchronously()
                .SetSerializer(new JsonMessageSerializer<ISettlementStatusChangedEvent>())
                .SetPublishStrategy(new DefaultFanoutPublishStrategy(settings))
                .Start();
        }

        public Task PublishAsync(IPaymentRequest paymentRequest)
        {
            return PublishAsync(_mapper.Map<SettlementStatusChangedEvent>(paymentRequest));
        }

        public async Task PublishAsync(ISettlementStatusChangedEvent settlementStatusChangedEvent)
        {
            await _publisher.ProduceAsync(settlementStatusChangedEvent);

            _log.Info("Settlement status changed event is published.", settlementStatusChangedEvent.ToJson());
        }

        public void Dispose()
        {
            _publisher?.Dispose();
        }

        public void Stop()
        {
            _publisher?.Stop();
        }
    }
}
