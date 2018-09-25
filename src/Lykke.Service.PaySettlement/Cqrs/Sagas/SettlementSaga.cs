using AutoMapper;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Contract.Events;
using Lykke.Service.PaySettlement.Contracts.Commands;
using Lykke.Service.PaySettlement.Contracts.Events;
using Lykke.Service.PaySettlement.Modules;
using System;

namespace Lykke.Service.PaySettlement.Cqrs.Sagas
{
    [UsedImplicitly]
    public class SettlementSaga
    {
        private readonly string _clientId;        
        private readonly IMapper _mapper;
        private readonly ILog _log;
        

        public SettlementSaga( string clientId, IMapper mapper, ILogFactory logFactory)
        {
            _clientId = clientId;
            _mapper = mapper;
            _log = logFactory.CreateLog(this);
        }

        [UsedImplicitly]
        public void Handle(PaymentRequestConfirmedEvent paymentRequestConfirmedEvent, ICommandSender commandSender)
        {
            commandSender.SendCommand(_mapper.Map<CreateSettlementCommand>(paymentRequestConfirmedEvent),
                CqrsModule.SettlementBoundedContext);
        }

        [UsedImplicitly]
        public void Handle(SettlementCreatedEvent createdEvent, ICommandSender commandSender)
        {
            if (createdEvent.IsError)
                return;

            commandSender.SendCommand(new TransferToMarketCommand
                {
                    PaymentRequestId = createdEvent.PaymentRequestId,
                    MerchantId = createdEvent.MerchantId
                },
                CqrsModule.SettlementBoundedContext);
        }

        public void Handle(CashinCompletedEvent cashinCompletedEvent, ICommandSender commandSender)
        {
            //todo: add off block chain prrocessing.
            if (!string.Equals(cashinCompletedEvent.ClientId.ToString(), _clientId, StringComparison.OrdinalIgnoreCase)
            || cashinCompletedEvent.OperationType != CashinOperationType.OnBlockchain )
            {
                return;
            }

            commandSender.SendCommand(_mapper.Map<ExchangeCommand>(cashinCompletedEvent), 
                CqrsModule.SettlementBoundedContext);
        }

        [UsedImplicitly]
        public void Handle(SettlementExchangedEvent exchangedEvent, ICommandSender commandSender)
        {
            commandSender.SendCommand(new TransferToMerchantCommand()
                {
                    PaymentRequestId = exchangedEvent.PaymentRequestId,
                    MerchantId = exchangedEvent.MerchantId
                },
                CqrsModule.SettlementBoundedContext);
        }
    }
}

