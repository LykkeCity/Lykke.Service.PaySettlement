using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Service.PaySettlement.Contracts.Commands;
using Lykke.Service.PaySettlement.Contracts.Events;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Core.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Cqrs.CommandHandlers
{
    [UsedImplicitly]
    public class ExchangeCommandHandler
    {
        private readonly IPaymentRequestService _paymentRequestService;
        private readonly IExchangeService _exchangeService;
        private readonly IErrorProcessHelper _errorProcessHelper;
        private readonly ILog _log;

        public ExchangeCommandHandler(IPaymentRequestService paymentRequestService, IExchangeService exchangeService, 
            IErrorProcessHelper errorProcessHelper, ILogFactory logFactory)
        {
            _paymentRequestService = paymentRequestService;
            _exchangeService = exchangeService;
            _errorProcessHelper = errorProcessHelper;
            _log = logFactory.CreateLog(this);
        }

        [UsedImplicitly]
        public async Task Handle(ExchangeCommand command, IEventPublisher publisher)
        {
            IPaymentRequest[] paymentRequests = (await _paymentRequestService.GetByTransferToMarketTransactionHash(
                    command.TransactionHash)).ToArray();
            if (!paymentRequests.Any())
            {
                return;
            }

            decimal totalTransacionAmount = paymentRequests.Sum(r => r.PaidAmount);
            foreach (IPaymentRequest paymentRequest in paymentRequests)
            {
                try
                {
                    IExchangeOrder exchangeOrder = await _exchangeService.AddToQueueAsync(paymentRequest,
                        totalTransacionAmount,
                        command.TransactionFee);

                    await _paymentRequestService.SetExchangeQueuedAsync(exchangeOrder, command.TransactionFee);

                    publisher.PublishEvent(new SettlementExchangeQueuedEvent
                    {
                        PaymentRequestId = paymentRequest.PaymentRequestId,
                        MerchantId = paymentRequest.MerchantId,
                        TransactionHash = command.TransactionHash,
                        TransactionFee = command.TransactionFee,
                        MarketAmount = exchangeOrder.Volume,
                        AssetId = exchangeOrder.SettlementAssetId
                    });
                }
                catch (Exception ex)
                {
                    await _errorProcessHelper.ProcessErrorAsync(paymentRequest.MerchantId, paymentRequest.PaymentRequestId,
                        publisher, true, "Unknown error has occured on adding to exchange queue.", ex);

                    throw;
                }
            }
        }
    }
}
