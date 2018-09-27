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
using Lykke.Service.PaySettlement.Core.Exceptions;

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

        public ExchangeCommandHandler(IPaymentRequestService paymentRequestService, 
            IExchangeService exchangeService, string multisigWalletAddress, 
            INinjaClient ninjaClient, CqrsBlockchainCashinDetectorSettings settings,
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
            string hash = command.TransactionHash;
            try
            {
                var transactionToHotWallet = await _ninjaClient.GetTransactionAsync(command.TransactionHash);
                if (transactionToHotWallet == null)
                {
                    await SetTransactionErrorAsync(command.TransactionHash,
                        SettlementProcessingError.NoTransactionDetails,
                        "Can not receive transaction details.", publisher);
                    return;
                }

                foreach (var coin in transactionToHotWallet.SpentCoins)
                {
                    if (!_multisigWalletAddress.Equals(coin.TxOut.ScriptPubKey.GetDestinationAddress(_ninjaNetwork)
                        ?.ToString()))
                    {
                        continue;
                    }

                    hash = coin.Outpoint.Hash.ToString();
                    await ProcessTransactionToMultisigWalletAsync(hash, publisher);
                }
            }
            catch (Exception ex)
            {
                await SetTransactionErrorAsync(hash, SettlementProcessingError.Unknown,
                    "Process transaction is failed.", publisher, ex);
                throw;
            }
        }

        public async Task ProcessTransactionToMultisigWalletAsync(string transactionToMultisigWalletHash,
            IEventPublisher publisher)
        {
            var transactionToMultisigWalletInfo = new TransactionInfo
            {
                Hash = transactionToMultisigWalletHash
            };
            var transactionToMultisigWallet = await _ninjaClient.GetTransactionAsync(
                transactionToMultisigWalletInfo.Hash);
            if (transactionToMultisigWallet == null)
            {
                await SetTransactionErrorAsync(transactionToMultisigWalletInfo.Hash,
                    SettlementProcessingError.NoTransactionDetails,
                    "Can not receive transaction details.", publisher);
                return;
            }

            transactionToMultisigWalletInfo.Amount =
                transactionToMultisigWallet.Transaction.TotalOut.ToDecimal(MoneyUnit.BTC);
            transactionToMultisigWalletInfo.Fee = transactionToMultisigWallet.Fees.ToDecimal(MoneyUnit.BTC);

            _log.Info("Lykke Pay to Multisig transaction is executed. \r\n" +
                      $"Transaction to multisig wallet is {transactionToMultisigWalletInfo.ToJson()}",
                new {TransactionHash = transactionToMultisigWalletInfo.Hash});

            await ProcessTransactionAsync(transactionToMultisigWalletInfo, publisher);
        }

        public async Task ProcessTransactionAsync(TransactionInfo transactionToMultisigWalletInfo,
            IEventPublisher publisher)
        {
            IPaymentRequest[] paymentRequests = (await _paymentRequestService.GetByTransferToMarketTransactionHash(
                transactionToMultisigWalletInfo.Hash)).ToArray();

            if (!paymentRequests.Any())
            {
                return;
            }

            decimal totalTransacionAmount =
                transactionToMultisigWalletInfo.Amount + transactionToMultisigWalletInfo.Fee;
            foreach (IPaymentRequest paymentRequest in paymentRequests)
            {
                var transactionComplexInfo = new TransactionInfo
                {
                    Hash = transactionToMultisigWalletInfo.Hash,
                    Amount = paymentRequest.PaidAmount -
                             paymentRequest.PaidAmount * transactionToMultisigWalletInfo.Fee / totalTransacionAmount,
                    Fee = transactionToMultisigWalletInfo.Fee
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

                await _paymentRequestService.SetExchangeQueuedAsync(exchangeOrder,
                    transactionComplexInfo.Fee);

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
            catch (SettlementException ex)
            {
                await _errorProcessHelper.ProcessErrorAsync(ex, publisher);
            }            
        }

        private async Task SetTransactionErrorAsync(string transactionHash, SettlementProcessingError error,
            string errorMessage, IEventPublisher publisher, Exception innerException = null)
        {
            try
            {
                _log.Error(innerException, errorMessage, new
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
                    var settlementException = new SettlementException(paymentRequest.MerchantId,
                        paymentRequest.PaymentRequestId, error, errorMessage, innerException);
                    await _errorProcessHelper.ProcessErrorAsync(settlementException, publisher);
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
