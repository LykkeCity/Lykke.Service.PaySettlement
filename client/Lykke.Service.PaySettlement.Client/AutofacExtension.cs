using Autofac;
using JetBrains.Annotations;
using Lykke.HttpClientGenerator;
using Lykke.HttpClientGenerator.Infrastructure;
using System;

namespace Lykke.Service.PaySettlement.Client
{
    [PublicAPI]
    public static class AutofacExtension
    {
        /// <summary>
        /// Registers <see cref="IPaySettlementClient"/> in Autofac container using <see cref="PaySettlementServiceClientSettings"/>.
        /// </summary>
        /// <param name="builder">Autofac container builder.</param>
        /// <param name="settings">PaySettlement client settings.</param>
        /// <param name="builderConfigure">Optional <see cref="HttpClientGeneratorBuilder"/> configure handler.</param>
        public static void RegisterPaySettlementClient(
            [NotNull] this ContainerBuilder builder,
            [NotNull] PaySettlementServiceClientSettings settings,
            [CanBeNull] Func<HttpClientGeneratorBuilder, HttpClientGeneratorBuilder> builderConfigure)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrWhiteSpace(settings.ServiceUrl))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(PaySettlementServiceClientSettings.ServiceUrl));

            var clientBuilder = HttpClientGenerator.HttpClientGenerator.BuildForUrl(settings.ServiceUrl)
                .WithAdditionalCallsWrapper(new ExceptionHandlerCallsWrapper());

            clientBuilder = builderConfigure?.Invoke(clientBuilder) ?? clientBuilder.WithoutRetries();

            builder.RegisterInstance(new PaySettlementClient(clientBuilder.Create()))
                .As<IPaySettlementClient>()
                .SingleInstance();
        }
    }
}
