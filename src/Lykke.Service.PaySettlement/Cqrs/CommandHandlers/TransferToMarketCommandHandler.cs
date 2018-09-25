using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Service.PaySettlement.Contracts.Commands;
using Lykke.Service.PaySettlement.Contracts.Events;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Core.Services;
using System;
using System.Threading.Tasks;
using Lykke.Service.PaySettlement.Cqrs.Helpers;

namespace Lykke.Service.PaySettlement.Cqrs.CommandHandlers
{
    [UsedImplicitly]
    public class TransferToMarketCommandHandler
    {
        private readonly IPaymentRequestService _paymentRequestService;
        private readonly ITransferToMarketService _transferToMarketService;
        private readonly IErrorProcessHelper _errorProcessHelper;
        private readonly ILog _log;

        public TransferToMarketCommandHandler(IPaymentRequestService paymentRequestService,
            ITransferToMarketService transferToMarketService, IErrorProcessHelper errorProcessHelper,
            ILogFactory logFactory)
        {
            _paymentRequestService = paymentRequestService;
            _transferToMarketService = transferToMarketService;
            _errorProcessHelper = errorProcessHelper;
            _log = logFactory.CreateLog(this);
        }

        [UsedImplicitly]
        public async Task Handle(TransferToMarketCommand command, IEventPublisher publisher)
        {
            try
            {
                IPaymentRequest paymentRequest =
                    await _paymentRequestService.GetAsync(command.MerchantId, command.PaymentRequestId);

                await _transferToMarketService.AddToQueueAsync(paymentRequest);

                await _paymentRequestService.SetTransferToMarketQueuedAsync(
                    command.MerchantId, command.PaymentRequestId);

                publisher.PublishEvent(new SettlementTransferToMarketQueuedEvent
                {
                    PaymentRequestId = paymentRequest.PaymentRequestId,
                    MerchantId = paymentRequest.MerchantId
                });
            }
            catch (Exception ex)
            {
                await _errorProcessHelper.ProcessUnknownErrorAsync(command, publisher, true, ex, 
                    "Unknown error has occured on adding to transfer to market queue.");
                throw;
            }
        }
    }
}
