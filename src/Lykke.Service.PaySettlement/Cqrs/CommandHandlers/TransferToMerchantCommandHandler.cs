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
using Lykke.Service.PaySettlement.Models.Exceptions;

namespace Lykke.Service.PaySettlement.Cqrs.CommandHandlers
{
    [UsedImplicitly]
    public class TransferToMerchantCommandHandler
    {
        private readonly IPaymentRequestService _paymentRequestService;
        private readonly ITransferToMerchantLykkeWalletService _transferToMerchantLykkeWalletService;
        private readonly IErrorProcessHelper _errorProcessHelper;
        private readonly ILog _log;

        public TransferToMerchantCommandHandler(IPaymentRequestService paymentRequestService,
            ITransferToMerchantLykkeWalletService transferToMerchantLykkeWalletService,
            IErrorProcessHelper errorProcessHelper, ILogFactory logFactory)
        {
            _paymentRequestService = paymentRequestService;
            _transferToMerchantLykkeWalletService = transferToMerchantLykkeWalletService;
            _errorProcessHelper = errorProcessHelper;
            _log = logFactory.CreateLog(this);
        }

        [UsedImplicitly]
        public async Task Handle(TransferToMerchantCommand command, IEventPublisher publisher)
        {
            try
            {
                IPaymentRequest paymentRequest =
                    await _paymentRequestService.GetAsync(command.MerchantId, command.PaymentRequestId);

                //If the first message is timeouted then possibility of the double transfer exists.
                if (paymentRequest.SettlementStatus != SettlementStatus.Exchanged)
                {
                    return;
                }

                TransferToMerchantResult transferToMerchantResult =
                    await _transferToMerchantLykkeWalletService.TransferAsync(paymentRequest);

                if (transferToMerchantResult.Error == SettlementProcessingError.None)
                {
                    await _paymentRequestService.SetTransferredToMerchantAsync(command.MerchantId,
                        command.PaymentRequestId, transferToMerchantResult.Amount);

                    publisher.PublishEvent(new SettlementTransferredToMerchantEvent
                    {
                        PaymentRequestId = paymentRequest.PaymentRequestId,
                        MerchantId = paymentRequest.MerchantId,
                        TransferredAmount = transferToMerchantResult.Amount,
                        TransferredAssetId = transferToMerchantResult.AssetId
                    });
                }
                else
                {
                    var settlementException = new SettlementException(command.MerchantId, command.PaymentRequestId,
                        transferToMerchantResult.Error, transferToMerchantResult.ErrorMessage);
                    await _errorProcessHelper.ProcessErrorAsync(settlementException, publisher);
                }
            }
            catch (Exception ex)
            {
                await _errorProcessHelper.ProcessUnknownErrorAsync(command, publisher, ex,
                    "Unknown error has occured on transferring to merchant Lykke wallet.");
                throw;
            }
        }
    }
}
