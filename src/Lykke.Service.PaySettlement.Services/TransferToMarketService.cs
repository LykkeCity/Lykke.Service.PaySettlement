using Autofac;
using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.PayInternal.Client;
using Lykke.Service.PayInternal.Client.Models.PaymentRequest;
using Lykke.Service.PayMerchant.Client;
using Lykke.Service.PayMerchant.Client.Models;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Core.Services;
using Lykke.Service.PaySettlement.Core.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using PaymentRequestStatus = Lykke.Service.PayInternal.Contract.PaymentRequest.PaymentRequestStatus;

namespace Lykke.Service.PaySettlement.Services
{
    public class TransferToMarketService : IStartable, IStopable, ITransferToMarketService
    {
        private readonly ITransferToMarketQueue _transferToMarketQueue;        
        private readonly IPayInternalClient _payInternalClient;
        private readonly IPayMerchantClient _payMerchantClient;
        private readonly IPaymentRequestsRepository _paymentRequestsRepository;
        private readonly IStatusService _statusService;
        private readonly TransferToMarketServiceSettings _settings;
        private readonly ILog _log;
        private readonly Timer _timer;

        public TransferToMarketService(ITransferToMarketQueue transferToMarketQueue,
            IPayInternalClient payInternalClient, IPayMerchantClient payMerchantClient, 
            IPaymentRequestsRepository paymentRequestsRepository, IStatusService statusService,
            TransferToMarketServiceSettings settings, ILogFactory logFactory)
        {
            _transferToMarketQueue = transferToMarketQueue;
            _payInternalClient = payInternalClient;
            _payMerchantClient = payMerchantClient;
            _paymentRequestsRepository = paymentRequestsRepository;
            _statusService = statusService;
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
                    new {PaymentRequestId = paymentRequest.Id});
                return;
            }

            if (!_settings.SettlementAssetIds.Contains(paymentRequest.SettlementAssetId,
                StringComparer.OrdinalIgnoreCase))
            {
                _log.Info($"Skip payment request because settlement assetId is {paymentRequest.PaymentAssetId}.",
                    new {PaymentRequestId = paymentRequest.Id});
                return;
            }

            MerchantModel merchant = await _payMerchantClient.Api.GetByIdAsync(paymentRequest.MerchantId);
            if (merchant == null)
            {
                _log.Error(null, $"Merchant {paymentRequest.MerchantId} is not found.",
                    new {PaymentRequestId = paymentRequest.Id});
                return;
            }

            if (string.IsNullOrEmpty(merchant.LwId))
            {
                _log.Info("Skip payment request because merchant Lykke wallet is not set.",
                    new {PaymentRequestId = paymentRequest.Id});
                return;
            }

            paymentRequest.SettlementStatus = SettlementStatus.TransferToMarketQueued;
            paymentRequest.MerchantClientId = merchant.LwId;

            await _paymentRequestsRepository.InsertOrMergeAsync(paymentRequest);

            await _statusService.SetTransferToMarketQueuedAsync(paymentRequest.MerchantId, 
                paymentRequest.Id);
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
                          $"TransactionHash: {response.Hash}. Payment requests:\r\n" +
                          $"{messages.ToJson()}");

                var tasks = new List<Task>();
                foreach (TransferToMarketMessage message in messages)
                {
                    //Fee is zero here.
                    tasks.Add(_statusService.SetTransferringToMarketAsync(message.MerchantId,
                        message.PaymentRequestId, response.Hash));
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
