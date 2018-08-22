using Autofac;
using Lykke.Common.Chaos;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Messaging;
using Lykke.Messaging.RabbitMq;
using Lykke.Messaging.Serialization;
using Lykke.Service.PaySettlement.Cqrs;
using Lykke.Service.PaySettlement.Settings;
using Lykke.SettingsReader;
using System.Collections.Generic;

namespace Lykke.Service.PaySettlement.Modules
{
    internal class CqrsModule : Module
    {
        private readonly IReloadingManager<AppSettings> _appSettings;

        public CqrsModule(IReloadingManager<AppSettings> appSettings)
        {
            _appSettings = appSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            if (_appSettings.CurrentValue.PaySettlementService.ChaosKitty != null)
            {
                builder
                    .RegisterType<ChaosKitty>()
                    .WithParameter(TypedParameter.From(_appSettings.CurrentValue.PaySettlementService.ChaosKitty.StateOfChaos))
                    .As<IChaosKitty>()
                    .SingleInstance();
            }
            else
            {
                builder
                    .RegisterType<SilentChaosKitty>()
                    .As<IChaosKitty>()
                    .SingleInstance();
            }

            MessagePackSerializerFactory.Defaults.FormatterResolver = MessagePack.Resolvers.ContractlessStandardResolver.Instance;

            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>().SingleInstance();

            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory { Uri = _appSettings.CurrentValue.PaySettlementService.RabbitMqSubscriber.ConnectionString };

            builder.RegisterType<TransactionProjection>()
                .WithParameter("multisigWalletAddress", _appSettings.CurrentValue.PaySettlementService.TransferToMarketService.MultisigWalletAddress)
                .WithParameter("isMainNet", _appSettings.CurrentValue.PaySettlementService.IsMainNet);

            builder.Register(ctx =>
            {
                var logFactory = ctx.Resolve<ILogFactory>();
#if DEBUG
                var broker = rabbitMqSettings.Endpoint + "/debug";
#else
                var broker = rabbitMqSettings.Endpoint.ToString();
#endif
                var messagingEngine = new MessagingEngine(logFactory,
                    new TransportResolver(new Dictionary<string, TransportInfo>
                    {
                        {"RabbitMq", new TransportInfo(broker, rabbitMqSettings.UserName, rabbitMqSettings.Password, "None", "RabbitMq")}
                    }),
                    new RabbitMqTransportFactory(logFactory));

                return new CqrsEngine(logFactory,
                    ctx.Resolve<IDependencyResolver>(),
                    messagingEngine,
                    new DefaultEndpointProvider(),
                    true,
                    Register.DefaultEndpointResolver(new RabbitMqConventionEndpointResolver(
                        "RabbitMq",
                        SerializationFormat.MessagePack,
                        environment: "lykke")),

                Register.BoundedContext("paysettlement")
                    .ListeningEvents(typeof(TransactionEvent))
                    .From("transactions").On("transactions-events")
                    .WithProjection(typeof(TransactionProjection), "transactions")
                );
            })
            .As<ICqrsEngine>().SingleInstance();
        }
    }
}
