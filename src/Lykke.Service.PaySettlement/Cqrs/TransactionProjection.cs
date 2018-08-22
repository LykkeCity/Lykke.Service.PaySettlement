using System;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.PaySettlement.Core.Services;
using System.Threading.Tasks;
using Lykke.Service.PaySettlement.Core.Domain;
using NBitcoin;

namespace Lykke.Service.PaySettlement.Cqrs
{
    public class TransactionProjection
    {
        private readonly ITradeService _tradeService;
        private readonly INinjaClient _ninjaClient;
        private readonly Network _ninjaNetwork;
        private readonly string _multisigWalletAddress;
        private readonly ILog _log;
        private const MoneyUnit MoneyUnit = NBitcoin.MoneyUnit.BTC;

        public TransactionProjection(ITradeService tradeService, 
            INinjaClient ninjaClient, string multisigWalletAddress, bool isMainNet, 
            ILogFactory logFactory)
        {
            _tradeService = tradeService;
            _ninjaClient = ninjaClient;
            _ninjaNetwork = isMainNet ? Network.Main : Network.TestNet;
            _multisigWalletAddress = multisigWalletAddress;
            _log = logFactory.CreateLog(this);
        }

        public async Task Handle(TransactionEvent transactionEvent)
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

                    _log.Info(
                        $"Multisig to Hot Wallet transaction is caught. Hash is {transactionEvent.TransactionHash}.",
                        new
                        {
                            TransactionId = transaction.TransactionId.ToString()
                        });

                    var transferToMarketTransaction =
                        await _ninjaClient.GetTransactionAsync(rec.Outpoint.Hash.ToString());

                    string transferToMarketTransactionId = transferToMarketTransaction.TransactionId.ToString();
                    decimal fee = transferToMarketTransaction.Fees.ToDecimal(MoneyUnit)
                                    + transaction.Fees.ToDecimal(MoneyUnit);//todo: check fee calculation

                    _log.Info($"Lykke Pay to Multisig transaction is executed. Fee is {fee}.",
                        new
                        {
                            TransactionId = transferToMarketTransaction.TransactionId.ToString()
                        });

                    await _tradeService.AddToQueueIfTransferred(transferToMarketTransactionId, fee);
                }
            }
            catch (Exception e)
            {
                _log.Error(e, $"Process transaction is failed. Hash is {transactionEvent.TransactionHash}.");
            }
        }
    }
}

