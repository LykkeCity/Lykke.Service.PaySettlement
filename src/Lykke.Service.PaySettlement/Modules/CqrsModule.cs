﻿using Autofac;
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
            builder
                .RegisterType<SilentChaosKitty>()
                .As<IChaosKitty>()
                .SingleInstance();

            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>().SingleInstance();

            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = _appSettings.CurrentValue.PaySettlementService.PaymentRequestsSubscriber.ConnectionString
            };

            builder.RegisterType<ConfirmationsSaga>()
                .WithParameter("multisigWalletAddress", _appSettings.CurrentValue.PaySettlementService.TransferToMarketService.MultisigWalletAddress)
                .WithParameter("isMainNet", _appSettings.CurrentValue.PaySettlementService.IsMainNet);

            builder.Register(ctx =>
            {
                var logFactory = ctx.Resolve<ILogFactory>();

                var broker = rabbitMqSettings.Endpoint.ToString();

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
                        SerializationFormat.ProtoBuf,
                        environment: _appSettings.CurrentValue.PaySettlementService.CqrsEnvironment)),

                    Register.Saga<ConfirmationsSaga>("paysettlement-transactions-saga")
                        .ListeningEvents(typeof(ConfirmationSavedEvent))
                        .From("transactions").On("transactions-events")
                );
            })
            .As<ICqrsEngine>().SingleInstance().AutoActivate();
        }
    }
}
