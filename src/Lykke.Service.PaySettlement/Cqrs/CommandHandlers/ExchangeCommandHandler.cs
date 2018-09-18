using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Service.PaySettlement.Contracts.Commands;
using Lykke.Service.PaySettlement.Contracts.Events;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Core.Services;
using Lykke.Service.PaySettlement.Cqrs.Helpers;
using Lykke.Service.PaySettlement.Settings;
using NBitcoin;
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
        private readonly INinjaClient _ninjaClient;
        private readonly Network _ninjaNetwork;
        private readonly string _multisigWalletAddress;
        private readonly ILog _log;

        public ExchangeCommandHandler(IPaymentRequestService paymentRequestService, IExchangeService exchangeService,
            string multisigWalletAddress, INinjaClient ninjaClient, CqrsBlockchainCashinDetectorSettings settings,
            IErrorProcessHelper errorProcessHelper, ILogFactory logFactory)
        {
            _paymentRequestService = paymentRequestService;
            _exchangeService = exchangeService;
            _errorProcessHelper = errorProcessHelper;
            _multisigWalletAddress = multisigWalletAddress;
            _ninjaClient = ninjaClient;
            _ninjaNetwork = settings.IsMainNet ? Network.Main : Network.TestNet;
            _log = logFactory.CreateLog(this);
        }

        [UsedImplicitly]
        public async Task Handle(ExchangeCommand command, IEventPublisher publisher)
        {
            try
            {
                var transactionToHotWallet = await _ninjaClient.GetTransactionAsync(command.TransactionHash);
                if (transactionToHotWallet == null)
                {
                    await SetTransactionErrorAsync(command.TransactionHash,
                        "Can not receive transaction details.", publisher);
                    return;
                }

                var transactionToHotWalletInfo = new TransactionInfo
                {
                    Hash = command.TransactionHash,
                    Amount = command.TransactionAmount,
                    Fee = command.TransactionFee
                };

                foreach (var coin in transactionToHotWallet.SpentCoins)
                {
                    if (!_multisigWalletAddress.Equals(coin.TxOut.ScriptPubKey.GetDestinationAddress(_ninjaNetwork)
                        ?.ToString()))
                    {
                        continue;
                    }

                    await ProcessTransactionToMultisigWalletAsync(transactionToHotWalletInfo, coin, publisher);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Process transaction is failed.",
                    new { TransactionHash = command.TransactionHash });
                throw;
            }
        }

        public async Task ProcessTransactionToMultisigWalletAsync(TransactionInfo transactionToHotWalletInfo, 
            ICoin coin, IEventPublisher publisher)
        {
            var transactionToMultisigWalletInfo = new TransactionInfo
            {
                Hash = coin.Outpoint.Hash.ToString()
            };
            var transactionToMultisigWallet = await _ninjaClient.GetTransactionAsync(
                transactionToMultisigWalletInfo.Hash);
            if (transactionToMultisigWallet == null)
            {
                await SetTransactionErrorAsync(transactionToHotWalletInfo.Hash,
                    "Can not receive transaction details.", publisher);
                return;
            }

            transactionToMultisigWalletInfo.Amount =
                transactionToMultisigWallet.Transaction.TotalOut.ToDecimal(MoneyUnit.BTC);
            transactionToMultisigWalletInfo.Fee = transactionToMultisigWallet.Fees.ToDecimal(MoneyUnit.BTC);

            decimal transactionToHotWalletFeePart =
                transactionToMultisigWalletInfo.Amount * transactionToHotWalletInfo.Fee /
                transactionToHotWalletInfo.Amount;
            
            decimal totalFee = transactionToHotWalletFeePart + transactionToMultisigWalletInfo.Fee;

            _log.Info("Lykke Pay to Multisig transaction is executed. \r\n" +
                      $"Total fee is {totalFee}.\r\n" +
                      $"Transaction to multisig wallet is {transactionToMultisigWalletInfo.ToJson()}\r\n" +
                      $"Transaction to hot wallet is {transactionToHotWalletInfo.ToJson()}",
                new { TransactionHash = transactionToMultisigWalletInfo.Hash });

            await ProcessTransactionAsync(transactionToMultisigWalletInfo, totalFee, publisher);
        }

        public async Task ProcessTransactionAsync(TransactionInfo transactionToMultisigWalletInfo, 
            decimal totalFee, IEventPublisher publisher)
        {
            IPaymentRequest[] paymentRequests;
            try
            {
                paymentRequests = (await _paymentRequestService.GetByTransferToMarketTransactionHash(
                    transactionToMultisigWalletInfo.Hash)).ToArray();

                if (!paymentRequests.Any())
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Unknown error has occured on adding to exchange queue.", new
                {
                    TransactionHash = transactionToMultisigWalletInfo.Hash
                });
                throw;
            }

            decimal totalTransacionAmount = transactionToMultisigWalletInfo.Amount + transactionToMultisigWalletInfo.Fee;
            foreach (IPaymentRequest paymentRequest in paymentRequests)
            {
                var transactionComplexInfo = new TransactionInfo
                {
                    Hash = transactionToMultisigWalletInfo.Hash,
                    Amount = paymentRequest.PaidAmount -
                             paymentRequest.PaidAmount * transactionToMultisigWalletInfo.Fee / totalTransacionAmount,
                    Fee = transactionToMultisigWalletInfo.Fee
                    //Amount = paymentRequest.PaidAmount -
                    //         paymentRequest.PaidAmount * totalFee / totalTransacionAmount,
                    //Fee = totalFee
                };
                await ProcessPaymentRequestAsync(paymentRequest, transactionComplexInfo, publisher);
            }
        }

        private async Task ProcessPaymentRequestAsync(IPaymentRequest paymentRequest,
            TransactionInfo transactionComplexInfo, IEventPublisher publisher)
        {
            try
            {
                IExchangeOrder exchangeOrder = await _exchangeService.AddToQueueAsync(paymentRequest,
                    transactionComplexInfo.Amount);

                await _paymentRequestService.SetExchangeQueuedAsync(exchangeOrder, transactionComplexInfo.Fee);

                publisher.PublishEvent(new SettlementExchangeQueuedEvent
                {
                    PaymentRequestId = paymentRequest.PaymentRequestId,
                    MerchantId = paymentRequest.MerchantId,
                    TransactionHash = transactionComplexInfo.Hash,
                    TransactionFee = transactionComplexInfo.Fee,
                    MarketAmount = exchangeOrder.Volume,
                    AssetId = exchangeOrder.SettlementAssetId
                });
            }
            catch (Exception ex)
            {
                await _errorProcessHelper.ProcessErrorAsync(paymentRequest.MerchantId,
                    paymentRequest.PaymentRequestId,
                    publisher, true, "Unknown error has occured on adding to exchange queue.", ex);

                throw;
            }
        }

        private async Task SetTransactionErrorAsync(string transactionHash, string errorMessage,
            IEventPublisher publisher)
        {
            try
            {
                _log.Error(null, errorMessage, new
                {
                    TransactionHash = transactionHash
                });

                IPaymentRequest[] paymentRequests = (await _paymentRequestService.GetByTransferToMarketTransactionHash(
                    transactionHash)).ToArray();
                if (!paymentRequests.Any())
                {
                    return;
                }

                foreach (IPaymentRequest paymentRequest in paymentRequests)
                {
                    await _errorProcessHelper.ProcessErrorAsync(paymentRequest.MerchantId,
                        paymentRequest.PaymentRequestId, publisher, true,
                        errorMessage);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Unknown error has occured on setting transferring to market error.", new
                {
                    TransactionHash = transactionHash
                });
            }
        }
    }
}
