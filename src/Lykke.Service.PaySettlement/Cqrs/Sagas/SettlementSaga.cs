using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.PaySettlement.Core.Services;
using NBitcoin;
using System;
using System.Threading.Tasks;
using AutoMapper;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Contract.Events;
using Lykke.Service.PaySettlement.Contracts.Commands;
using Lykke.Service.PaySettlement.Contracts.Events;
using Lykke.Service.PaySettlement.Modules;
using Lykke.Service.PaySettlement.Settings;

namespace Lykke.Service.PaySettlement.Cqrs.Sagas
{
    [UsedImplicitly]
    public class SettlementSaga
    {
        private readonly INinjaClient _ninjaClient;
        private readonly Network _ninjaNetwork;
        private readonly string _multisigWalletAddress;
        private readonly string _clientId;
        private readonly IMapper _mapper;
        private readonly ILog _log;
        

        public SettlementSaga(INinjaClient ninjaClient, string multisigWalletAddress, string clientId,
            IMapper mapper, CqrsBlockchainCashinDetectorSettings settings, ILogFactory logFactory)
        {
            _ninjaClient = ninjaClient;
            _ninjaNetwork = settings.IsMainNet ? Network.Main : Network.TestNet;
            _multisigWalletAddress = multisigWalletAddress;
            _clientId = clientId;
            _mapper = mapper;
            _log = logFactory.CreateLog(this);
        }

        [UsedImplicitly]
        public void Handle(PaymentRequestDetailsEvent paymentRequestDetailsEvent, ICommandSender commandSender)
        {
            commandSender.SendCommand(_mapper.Map<CreateSettlementCommand>(paymentRequestDetailsEvent),
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

        public async Task Handle(CashinCompletedEvent cashinCompletedEvent, ICommandSender commandSender)
        {
            if (!string.Equals(cashinCompletedEvent.ClientId.ToString(), _clientId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                var transaction = await _ninjaClient.GetTransactionAsync(cashinCompletedEvent.TransactionHash);
                if (transaction == null)
                {
                    return;
                }

                foreach (var rec in transaction.ReceivedCoins)
                {
                    if (!_multisigWalletAddress.Equals(rec.TxOut.ScriptPubKey.GetDestinationAddress(_ninjaNetwork)
                        ?.ToString()))
                    {
                        continue;
                    }

                    decimal fee = transaction.Fees.ToDecimal(MoneyUnit.BTC);

                    _log.Info($"Lykke Pay to Multisig transaction is executed. Fee is {fee}.",
                        new {TransactionHash = cashinCompletedEvent.TransactionHash});

                    commandSender.SendCommand(new ExchangeCommand
                    {
                        TransactionHash = cashinCompletedEvent.TransactionHash,
                        TransactionFee = fee
                    }, CqrsModule.SettlementBoundedContext);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Process transaction is failed.",
                    new {TransactionHash = cashinCompletedEvent.TransactionHash});
                throw;
            }
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

