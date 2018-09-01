using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.PaySettlement.Core.Services;
using NBitcoin;
using System;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Cqrs
{
    public class ConfirmationsSaga
    {
        private readonly ITradeService _tradeService;
        private readonly INinjaClient _ninjaClient;
        private readonly Network _ninjaNetwork;
        private readonly string _multisigWalletAddress;
        private readonly ILog _log;
        private const MoneyUnit MoneyUnit = NBitcoin.MoneyUnit.BTC;

        public ConfirmationsSaga(ITradeService tradeService, 
            INinjaClient ninjaClient, string multisigWalletAddress, bool isMainNet, 
            ILogFactory logFactory)
        {
            _tradeService = tradeService;
            _ninjaClient = ninjaClient;
            _ninjaNetwork = isMainNet ? Network.Main : Network.TestNet;
            _multisigWalletAddress = multisigWalletAddress;
            _log = logFactory.CreateLog(this);
        }

        public async Task Handle(ConfirmationSavedEvent transactionEvent)
        {
            if (!string.Equals(transactionEvent.Multisig, _multisigWalletAddress, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                var transaction = await _ninjaClient.GetTransactionAsync(transactionEvent.TransactionHash);
                if (transaction == null)
                {
                    return;
                }

                foreach (var rec in transaction.ReceivedCoins)
                {
                    if (!transactionEvent.Multisig.Equals(rec.TxOut.ScriptPubKey.GetDestinationAddress(_ninjaNetwork)
                        ?.ToString()))
                    {
                        continue;
                    }

                    decimal fee = transaction.Fees.ToDecimal(MoneyUnit);

                    _log.Info($"Lykke Pay to Multisig transaction is executed. Fee is {fee}.",
                        new { TransactionHash = transactionEvent.TransactionHash });

                    await _tradeService.AddToQueueIfTransferredAsync(transactionEvent.TransactionHash, fee);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Process transaction is failed.",
                    new { TransactionHash = transactionEvent.TransactionHash });
                throw;
            }
        }
    }
}

