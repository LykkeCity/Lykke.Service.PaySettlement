using Autofac;
using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.PayInternal.Client;
using Lykke.Service.PayInternal.Client.Models.PaymentRequest;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Core.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Lykke.Service.PayInternal.Client.Models.Merchant;
using Lykke.Service.PaySettlement.Core.Services;
using PaymentRequestStatus = Lykke.Service.PayInternal.Contract.PaymentRequest.PaymentRequestStatus;

namespace Lykke.Service.PaySettlement.Services
{
    public class TransferToMarketService : IStartable, IStopable, ITransferToMarketService
    {
        private readonly ITransferToMarketQueue _transferToMarketQueue;
        private readonly TransferToMarketServiceSettings _settings;
        private readonly ILog _log;
        private readonly Timer _timer;
        private readonly IPayInternalClient _payInternalClient;
        private readonly IPaymentRequestsRepository _paymentRequestsRepository;

        public TransferToMarketService(ITransferToMarketQueue transferToMarketQueue, 
            IPayInternalClient payInternalClient, IPaymentRequestsRepository paymentRequestsRepository,
            TransferToMarketServiceSettings settings, ILogFactory logFactory)
        {
            _transferToMarketQueue = transferToMarketQueue;
            _payInternalClient = payInternalClient;
            _paymentRequestsRepository = paymentRequestsRepository;
            _settings = settings;
            _log = logFactory.CreateLog(this);
            _timer = new Timer(settings.Interval.TotalMilliseconds);            
        }

        public async Task AddToQueueIfSettlement(IPaymentRequest paymentRequest)
        {
            if (paymentRequest.Status != PaymentRequestStatus.Confirmed)
            {
                return;
            }

            if (!string.Equals(paymentRequest.PaymentAssetId, _settings.PaymentAssetId,
                StringComparison.OrdinalIgnoreCase))
            {
                _log.Info($"Skip payment request because payment assetId is {paymentRequest.PaymentAssetId}.",
                    new { PaymentRequestId = paymentRequest.Id });
                return;
            }

            if (!_settings.SettlementAssetIds.Contains(paymentRequest.SettlementAssetId,
                StringComparer.OrdinalIgnoreCase))
            {
                _log.Info($"Skip payment request because settlement assetId is {paymentRequest.PaymentAssetId}.",
                    new { PaymentRequestId = paymentRequest.Id });
                return;
            }

            MerchantModel merchant = await _payInternalClient.GetMerchantByIdAsync(paymentRequest.MerchantId);
            if (merchant == null)
            {
                _log.Error(null, $"Merchant {paymentRequest.MerchantId} is not found.",
                    new { PaymentRequestId = paymentRequest.Id });
                return;
            }

            paymentRequest.SettlementStatus = SettlementStatus.TransferToMarketQueued;
            paymentRequest.MerchantClientId = merchant.LwId;
            await _paymentRequestsRepository.InsertOrMergeAsync(paymentRequest);
            await _transferToMarketQueue.AddPaymentRequestsAsync(paymentRequest);

            _log.Info("Payment request is queued.", new { PaymentRequestId = paymentRequest.Id });
        }

        public void Start()
        {
            _timer.Elapsed += async (sender, e) => await TransferAsync();
            _timer.Start();
        }

        private async Task TransferAsync()
        {
            try
            {
                int processed = int.MaxValue;
                while (processed > 0)
                {
                    processed = await _transferToMarketQueue.ProcessTransferAsync(TransferAsync,
                        _settings.MaxTransfersPerTransaction);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        private async Task<bool> TransferAsync(TransferToMarketMessage[] messages)
        {
            try
            {
                if (!messages.Any())
                {
                    _log.Info("There are no payment requests for processing.");
                    return true;
                }

                var sources = new List<BtcTransferSourceInfo>(messages.Length);
                foreach (TransferToMarketMessage message in messages)
                {
                    sources.Add(new BtcTransferSourceInfo()
                    {
                        Address = message.PaymentRequestWalletAddress,
                        Amount = message.Amount
                    });
                }

                var transferRequest = new BtcFreeTransferRequest()
                {
                    DestAddress = _settings.MultisigWalletAddress,
                    Sources = sources.ToArray()
                };

                BtcTransferResponse response = await _payInternalClient.BtcFreeTransferAsync(transferRequest);
                _log.Info($"Transfer from Lykke Pay wallets to market multisig wallet {_settings.MultisigWalletAddress}. " +
                          $"TransactionId: {response.TransactionId}. Payment requests:\r\n" +
                          $"{messages.ToJson()}");

                var tasks = new List<Task>();
                foreach (TransferToMarketMessage message in messages)
                {
                    tasks.Add(_paymentRequestsRepository.SetTransferringToMarketAsync(message.PaymentRequestId, response.TransactionId));
                    _log.Info($"Payment request is transferring from {message.PaymentRequestWalletAddress} to {_settings.MultisigWalletAddress}.", new
                    {
                        TransactionId = response.TransactionId,
                        PaymentRequestId = message.PaymentRequestId
                    });
                }

                await Task.WhenAll(tasks);
                return true;
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Transfer is failed: {messages.ToJson()}");
                return false;
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public void Stop()
        {
            _timer?.Stop();
        }
    }
}
