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
using Common;
using Lykke.Service.PaySettlement.Contracts.Commands;
using Lykke.Service.PaySettlement.Contracts.Events;
using Lykke.Service.PaySettlement.Cqrs.CommandHandlers;
using Lykke.Service.PaySettlement.Cqrs.Events;
using Lykke.Service.PaySettlement.Cqrs.Helpers;
using Lykke.Service.PaySettlement.Cqrs.Processes;
using Lykke.Service.PaySettlement.Rabbit;
using Lykke.Service.PaySettlement.Cqrs.Sagas;

namespace Lykke.Service.PaySettlement.Modules
{
    internal class CqrsModule : Module
    {
        private readonly IReloadingManager<AppSettings> _appSettings;
        private const string CommandsRoute = "commands";
        private const string EventsRoute = "events";
        public const string SettlementBoundedContext = "lykkepay-settlement";

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

            RegisterComponents(builder);

            const string mainTransport = "RabbitMq";
            var mainSettings = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = _appSettings.CurrentValue.PaySettlementService.PaymentRequestsSubscriber.ConnectionString
            };

            const string txTransactionsTransport = "TxTransactionsRabbitMq";
            var txTransactionsSettings = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = _appSettings.CurrentValue.PaySettlementService.CqrsTxTransactions.ConnectionString
            };

            builder.Register(ctx =>
            {
                var logFactory = ctx.Resolve<ILogFactory>();

                var mainBroker = mainSettings.Endpoint.ToString();
                var txTransactionsBroker = txTransactionsSettings.Endpoint.ToString();

                var messagingEngine = new MessagingEngine(logFactory,
                    new TransportResolver(new Dictionary<string, TransportInfo>
                    {
                        {
                            mainTransport,
                            new TransportInfo(mainBroker, mainSettings.UserName, mainSettings.Password, "None",
                                "RabbitMq")
                        },
                        {
                            txTransactionsTransport,
                            new TransportInfo(txTransactionsBroker, txTransactionsSettings.UserName,
                                txTransactionsSettings.Password, "None", "RabbitMq")
                        }
                    }),
                    new RabbitMqTransportFactory(logFactory));

                return new CqrsEngine(logFactory,
                    ctx.Resolve<IDependencyResolver>(),
                    messagingEngine,
                    new DefaultEndpointProvider(),
                    true,
                    Register.DefaultEndpointResolver(new RabbitMqConventionEndpointResolver(
                        mainTransport,
                        SerializationFormat.ProtoBuf,
                        environment: _appSettings.CurrentValue.PaySettlementService.CqrsEnvironment)),

                    Register.BoundedContext(SettlementBoundedContext)
                        .PublishingEvents(typeof(PaymentRequestDetailsEvent))
                        .With(EventsRoute)
                        .WithProcess<PaymentRequestsSubscriber>()

                        .ListeningCommands(typeof(CreateSettlementCommand))
                        .On(CommandsRoute)
                        .PublishingEvents(typeof(SettlementCreatedEvent), typeof(SettlementErrorEvent))
                        .With(EventsRoute)
                        .WithCommandsHandler<CreateSettlementCommandHandler>()

                        .ListeningCommands(typeof(TransferToMarketCommand))
                        .On(CommandsRoute)
                        .PublishingEvents(typeof(SettlementTransferToMarketQueuedEvent), typeof(SettlementErrorEvent))
                        .With(EventsRoute)
                        .WithCommandsHandler<TransferToMarketCommandHandler>()

                        .PublishingEvents(typeof(SettlementTransferringToMarketEvent), typeof(SettlementErrorEvent))
                        .With(EventsRoute)
                        .WithProcess<TransferToMarketProcess>()

                        .ListeningCommands(typeof(ExchangeCommand))
                        .On(CommandsRoute)
                        .PublishingEvents(typeof(SettlementExchangeQueuedEvent), typeof(SettlementErrorEvent))
                        .With(EventsRoute)
                        .WithCommandsHandler<ExchangeCommandHandler>()

                        .PublishingEvents(typeof(SettlementExchangedEvent), typeof(SettlementErrorEvent))
                        .With(EventsRoute)
                        .WithProcess<ExchangeProcess>()

                        .ListeningCommands(typeof(TransferToMerchantCommand))
                        .On(CommandsRoute)
                        .PublishingEvents(typeof(SettlementTransferredToMerchantEvent), typeof(SettlementErrorEvent))
                        .With(EventsRoute)
                        .WithCommandsHandler<TransferToMerchantCommandHandler>(),

                    Register.Saga<SettlementSaga>("lykkepay-settlement-saga")
                        .PublishingCommands(typeof(CreateSettlementCommand),
                            typeof(TransferToMarketCommand), 
                            typeof(ExchangeCommand), 
                            typeof(TransferToMerchantCommand))
                        .To(SettlementBoundedContext).With(CommandsRoute)

                        .ListeningEvents(typeof(PaymentRequestDetailsEvent),
                            typeof(SettlementCreatedEvent),
                            typeof(SettlementExchangedEvent))
                        .From(SettlementBoundedContext).On(EventsRoute)

                        .ListeningEvents(typeof(ConfirmationSavedEvent))
                        .From("transactions").On("transactions-events")
                        .WithEndpointResolver(new RabbitMqConventionEndpointResolver(
                            txTransactionsTransport,
                            SerializationFormat.ProtoBuf,
                            environment: _appSettings.CurrentValue.PaySettlementService.CqrsTxTransactions.Environment))
                );
            })
            .As<ICqrsEngine>().SingleInstance().AutoActivate();
        }

        private void RegisterComponents(ContainerBuilder builder)
        {
            builder.RegisterType<ErrorProcessHelper>()
                .As<IErrorProcessHelper>();

            builder.RegisterType<PaymentRequestsSubscriber>()
                .WithParameter("settings", _appSettings.CurrentValue.PaySettlementService.PaymentRequestsSubscriber);

            builder.RegisterType<CreateSettlementCommandHandler>();
            builder.RegisterType<TransferToMarketCommandHandler>();
            builder.RegisterType<TransferToMarketProcess>()
                .WithParameter("interval", _appSettings.CurrentValue.PaySettlementService.TransferToMarketInterval)
                .AsSelf()
                .As<IStartable>()
                .As<IStopable>()
                .AutoActivate()
                .SingleInstance();
            builder.RegisterType<ExchangeCommandHandler>();
            builder.RegisterType<ExchangeProcess>()
                .WithParameter("interval", _appSettings.CurrentValue.PaySettlementService.ExchangeInterval)
                .AsSelf()
                .As<IStartable>()
                .As<IStopable>()
                .AutoActivate()
                .SingleInstance();
            builder.RegisterType<TransferToMerchantCommandHandler>();
            
            builder.RegisterType<SettlementSaga>()
                .WithParameter("multisigWalletAddress", _appSettings.CurrentValue.PaySettlementService.TransferToMarketService.MultisigWalletAddress)
                .WithParameter("settings", _appSettings.CurrentValue.PaySettlementService.CqrsTxTransactions);
        }
    }
}
