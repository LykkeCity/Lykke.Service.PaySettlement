using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Cqrs;
using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Service.PaySettlement.Contracts.Events;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Core.Services;
using Lykke.Service.PaySettlement.Cqrs.Helpers;
using Lykke.Service.PaySettlement.Models.Exceptions;

namespace Lykke.Service.PaySettlement.Cqrs.Processes
{
    [UsedImplicitly]
    public class TransferToMarketProcess : TimerPeriod, IProcess
    {
        private readonly ITransferToMarketService _transferToMarketService;
        private readonly IPaymentRequestService _paymentRequestService;
        private readonly IErrorProcessHelper _errorProcessHelper;
        private IEventPublisher _eventPublisher;
        private readonly ILog _log;

        public TransferToMarketProcess(ITransferToMarketService transferToMarketService,
            IPaymentRequestService paymentRequestService, TimeSpan interval,
            IErrorProcessHelper errorProcessHelper, ILogFactory logFactory) 
            : base(interval, logFactory)
        {
            _transferToMarketService = transferToMarketService;
            _paymentRequestService = paymentRequestService;
            _errorProcessHelper = errorProcessHelper;
            _log = logFactory.CreateLog(this);
        }

        [UsedImplicitly]
        public void Start(ICommandSender commandSender, IEventPublisher eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }

        public override async Task Execute()
        {
            if (_eventPublisher == null)
            {
                return;
            }

            TransferBatchPaymentRequestsResult result;
            do
            {
                result =
                    await _transferToMarketService.TransferBatchPaymentRequestsAsync();

                await ProcessTransferResultAsync(result);

            } while (result.IsSuccess && result.TransferToMarketMessages?.Any() == true);
        }

        private async Task ProcessTransferResultAsync(TransferBatchPaymentRequestsResult result)
        {
            if (result.TransferToMarketMessages == null)
            {
                return;
            }

            foreach (var transferToMarketMessage in result.TransferToMarketMessages)
            {
                if (result.IsSuccess)
                {
                    await _paymentRequestService.SetTransferringToMarketAsync(
                        transferToMarketMessage.MerchantId, transferToMarketMessage.PaymentRequestId,
                        result.TransactionHash);

                    _eventPublisher.PublishEvent(new SettlementTransferringToMarketEvent
                    {
                        PaymentRequestId = transferToMarketMessage.PaymentRequestId,
                        MerchantId = transferToMarketMessage.MerchantId,
                        TransactionHash = result.TransactionHash,
                        Amount = transferToMarketMessage.Amount,
                        AssetId = transferToMarketMessage.AssetId,
                        DestinationAddress = result.DestinationAddress
                    });
                }
                else
                {
                    var settlementException = new SettlementException(transferToMarketMessage.MerchantId,
                        transferToMarketMessage.PaymentRequestId, SettlementProcessingError.Unknown, 
                        result.ErrorMessage, result.Exception);

                    await _errorProcessHelper.ProcessErrorAsync(settlementException, _eventPublisher);
                }
            }
        }
    }
}
