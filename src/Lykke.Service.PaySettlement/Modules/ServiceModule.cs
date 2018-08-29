using System;
using Autofac;
using AutoMapper;
using AzureStorage.Queue;
using AzureStorage.Tables;
using AzureStorage.Tables.Templates.Index;
using Common;
using Lykke.Common.Log;
using Lykke.Service.Assets.Client;
using Lykke.Service.PaySettlement.AzureRepositories.PaymentRequests;
using Lykke.Service.PaySettlement.AzureRepositories.Trading;
using Lykke.Service.PaySettlement.AzureRepositories.TransferToMarket;
using Lykke.Service.PaySettlement.AzureRepositories.TransferToMerchant;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Core.Services;
using Lykke.Service.PaySettlement.Services;
using Lykke.Service.PaySettlement.Settings;
using Lykke.SettingsReader;
using QBitNinja.Client;
using System.Net;
using Lykke.Service.PaySettlement.Rabbit;
using Lykke.Service.Balances.Client;
using Lykke.Service.ExchangeOperations.Client;
using Lykke.Service.PayInternal.Client;
using Lykke.Service.PayMerchant.Client;

namespace Lykke.Service.PaySettlement.Modules
{
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<AppSettings> _appSettings;

        public ServiceModule(IReloadingManager<AppSettings> appSettings)
        {
            _appSettings = appSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            // Do not register entire settings in container, pass necessary settings to services which requires them
            var mapperProvider = new MapperProvider();
            IMapper mapper = mapperProvider.GetMapper();
            builder.RegisterInstance(mapper).As<IMapper>();

            RegisterMeClient(builder);
            builder.RegisterAssetsClient(AssetServiceSettings.Create(
                new Uri(_appSettings.CurrentValue.AssetsServiceClient.ServiceUrl), 
                _appSettings.CurrentValue.AssetsServiceClient.ExpirationPeriod));
            RegisterNinja(builder);
            RegisterRepositories(builder);

            builder.RegisterInstance<IExchangeOperationsServiceClient>(new ExchangeOperationsServiceClient(
                _appSettings.CurrentValue.ExchangeOperationsServiceClient.ServiceUrl));

            builder.RegisterType<PayInternalClient>()
                .As<IPayInternalClient>()
                .WithParameter(TypedParameter.From(_appSettings.CurrentValue.PayInternalServiceClient))
                .SingleInstance();

            builder.RegisterPayMerchantClient(_appSettings.CurrentValue.PayMerchantServiceClient, null);

            builder.RegisterType<AssetService>()
                .As<IAssetService>()
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance()
                .WithParameter("settings", _appSettings.CurrentValue.PaySettlementService.TransferToMarketService);

            builder.RegisterType<AccuracyRoundHelper>()
                .As<IAccuracyRoundHelper>();

            builder.RegisterType<StatusService>()
                .As<IStatusService>()
                .SingleInstance();

            builder.RegisterType<TradeService>()
                .As<ITradeService>()
                .As<IStartable>()
                .As<IStopable>()
                .AutoActivate()
                .SingleInstance()
                .WithParameter("settings", _appSettings.CurrentValue.PaySettlementService.TradeService);

            builder.RegisterType<TransferToMarketService>()
                .As<ITransferToMarketService>()
                .As<IStartable>()
                .As<IStopable>()
                .AutoActivate()
                .SingleInstance()
                .WithParameter("settings", _appSettings.CurrentValue.PaySettlementService.TransferToMarketService);

            builder.RegisterType<TransferToMerchantService>()
                .As<ITransferToMerchantService>()
                .As<IStartable>()
                .As<IStopable>()
                .AutoActivate()
                .SingleInstance()
                .WithParameter("settings", _appSettings.CurrentValue.PaySettlementService.TransferToMerchantService);

            builder.RegisterType<PaymentRequestsSubscriber>()
                .As<IStartable>()
                .As<IStopable>()
                .AutoActivate()
                .SingleInstance()
                .WithParameter("settings", _appSettings.CurrentValue.PaySettlementService.PaymentRequestsSubscriber);

            builder.RegisterType<SettlementStatusPublisher>()
                .As<ISettlementStatusPublisher>()
                .As<IStartable>()
                .As<IStopable>()
                .AutoActivate()
                .SingleInstance()
                .WithParameter("settings", _appSettings.CurrentValue.PaySettlementService.SettlementStatusPublisher);

            builder.RegisterType<LykkeBalanceService>()
                .As<ILykkeBalanceService>()
                .WithParameter("clientId", _appSettings.CurrentValue.PaySettlementService.TradeService.ClientId);

            builder.RegisterBalancesClient(_appSettings.CurrentValue.BalancesServiceClient);
        }

        private void RegisterMeClient(ContainerBuilder builder)
        {
            if (!IPAddress.TryParse(_appSettings.CurrentValue.MatchingEngineClient.IpEndpoint.Host, out var address))
                address = Dns.GetHostAddressesAsync(_appSettings.CurrentValue.MatchingEngineClient.IpEndpoint.Host).Result[0];

            var endPoint = new IPEndPoint(address, _appSettings.CurrentValue.MatchingEngineClient.IpEndpoint.Port);

            builder.RegisgterMeClient(endPoint);
        }

        private void RegisterNinja(ContainerBuilder builder)
        {
            builder.RegisterInstance(new QBitNinjaClient(_appSettings.CurrentValue.NinjaServiceClient.ServiceUrl))
                .AsSelf();

            builder.RegisterType<NinjaClient>()
                .As<INinjaClient>()
                .SingleInstance();
        }

        private void RegisterRepositories(ContainerBuilder builder)
        {
            builder.Register(c =>
                    new PaymentRequestsRepository(
                        AzureTableStorage<PaymentRequestEntity>.Create(
                            _appSettings.ConnectionString(x => x.PaySettlementService.Db.DataConnString),
                            _appSettings.CurrentValue.PaySettlementService.Db.PaymentRequestsTableName,
                            c.Resolve<ILogFactory>()),
                        AzureTableStorage<AzureIndex>.Create(
                            _appSettings.ConnectionString(x => x.PaySettlementService.Db.DataConnString),
                            _appSettings.CurrentValue.PaySettlementService.Db.PaymentRequestsTableName,
                            c.Resolve<ILogFactory>()),
                        AzureTableStorage<AzureIndex>.Create(
                            _appSettings.ConnectionString(x => x.PaySettlementService.Db.DataConnString),
                            _appSettings.CurrentValue.PaySettlementService.Db.PaymentRequestsTableName,
                            c.Resolve<ILogFactory>())))
                .As<IPaymentRequestsRepository>()
                .SingleInstance();

            builder.Register(c =>
                    new TradeOrdersRepository(
                        AzureTableStorage<TradeOrderEntity>.Create(
                            _appSettings.ConnectionString(x => x.PaySettlementService.Db.DataConnString),
                            _appSettings.CurrentValue.PaySettlementService.Db.TradeOrdersTableName,
                            c.Resolve<ILogFactory>())))
                .As<ITradeOrdersRepository>()
                .SingleInstance();

            builder.Register(c =>
                    new TransferToMarketQueue(
                        AzureQueueExt.Create(
                            _appSettings.ConnectionString(x => x.PaySettlementService.Db.DataConnString),
                            _appSettings.CurrentValue.PaySettlementService.Db.TransferToMarketQueue)))
                .As<ITransferToMarketQueue>()
                .SingleInstance();

            builder.Register(c =>
                    new TransferToMerchantQueue(
                        AzureQueueExt.Create(
                            _appSettings.ConnectionString(x => x.PaySettlementService.Db.DataConnString),
                            _appSettings.CurrentValue.PaySettlementService.Db.TransferToMerchantQueue)))
                .As<ITransferToMerchantQueue>()
                .SingleInstance();
        }
    }
}
