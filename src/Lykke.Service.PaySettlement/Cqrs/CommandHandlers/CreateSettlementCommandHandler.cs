using AutoMapper;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Service.PayMerchant.Client;
using Lykke.Service.PayMerchant.Client.Models;
using Lykke.Service.PaySettlement.Contracts.Commands;
using Lykke.Service.PaySettlement.Contracts.Events;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Core.Exceptions;
using Lykke.Service.PaySettlement.Core.Services;
using Lykke.Service.PaySettlement.Cqrs.Helpers;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Cqrs.CommandHandlers
{
    [UsedImplicitly]
    public class CreateSettlementCommandHandler
    {
        private readonly IPaymentRequestService _paymentRequestService;
        private readonly IPayMerchantClient _payMerchantClient;
        private readonly IAssetService _assetService;
        private readonly IErrorProcessHelper _errorProcessHelper;
        private readonly IMapper _mapper;
        private readonly ILog _log;

        public CreateSettlementCommandHandler(IPaymentRequestService paymentRequestService,
            IPayMerchantClient payMerchantClient, IAssetService assetService,
            IErrorProcessHelper errorProcessHelper, IMapper mapper, ILogFactory logFactory)
        {
            _paymentRequestService = paymentRequestService;
            _payMerchantClient = payMerchantClient;
            _assetService = assetService;
            _errorProcessHelper = errorProcessHelper;
            _mapper = mapper;
            _log = logFactory.CreateLog(this);
        }

        [UsedImplicitly]
        public async Task Handle(CreateSettlementCommand command, IEventPublisher publisher)
        {
            IPaymentRequest paymentRequest = null;
            try
            {
                paymentRequest = _mapper.Map<PaymentRequest>(command);

                if (!await ValidateCommandAsync(command))
                {
                    return;
                }

                paymentRequest.MerchantClientId = await GetMerchantClientIdAsync(command);

                if (!ValidatePaymentRequest(paymentRequest))
                {
                    return;
                }

                if (!ValidateMinAmount(paymentRequest, out string validateMessage))
                {
                    var settlemenetException = new SettlementException(paymentRequest.MerchantId,
                        paymentRequest.PaymentRequestId, SettlementProcessingError.LowAmount, 
                        validateMessage);

                    await TryAddPaymentRequestWithErrorAsync(paymentRequest,
                        settlemenetException.Error, publisher);
                    await _errorProcessHelper.ProcessErrorAsync(settlemenetException, publisher);
                    return;
                }
            }
            catch (SettlementException ex)
            {
                await TryAddPaymentRequestWithErrorAsync(paymentRequest, ex.Error, publisher);
                await _errorProcessHelper.ProcessErrorAsync(ex, publisher);
                return;
            }
            catch (Exception ex)
            {
                await TryAddPaymentRequestWithErrorAsync(paymentRequest, 
                    SettlementProcessingError.Unknown, publisher);
                await _errorProcessHelper.ProcessUnknownErrorAsync(command, publisher, ex,
                    "Unknown error has occured on validating.");
                throw;
            }

            try
            {
                await AddPaymentRequestAsync(paymentRequest, publisher);
            }
            catch (Exception ex)
            {
                await _errorProcessHelper.ProcessUnknownErrorAsync(command, publisher, ex,
                    "Unknown error has occured on adding payment request.");
                throw;
            }
        }

        private async Task<bool> ValidateCommandAsync(CreateSettlementCommand command)
        {
            if (!_assetService.IsPaymentAssetIdValid(command.PaymentAssetId))
            {
                _log.Info($"Skip payment request because payment assetId is {command.PaymentAssetId}.", new
                {
                    command.MerchantId,
                    command.PaymentRequestId
                });
                return false;
            }

            if (!_assetService.IsSettlementAssetIdValid(command.SettlementAssetId))
            {
                _log.Info($"Skip payment request because settlement assetId is {command.SettlementAssetId}.", new
                {
                    command.MerchantId,
                    command.PaymentRequestId
                });
                return false;
            }

            if (null != await _paymentRequestService.GetAsync(command.MerchantId, command.PaymentRequestId))
            {
                _log.Info("Skip duplicate payment request.", new
                {
                    command.MerchantId,
                    command.PaymentRequestId
                });
                return false;
            }

            if (string.IsNullOrEmpty(command.WalletAddress))
            {
                _log.Info($"Skip payment request because {nameof(command.WalletAddress)} is empty.", new
                {
                    command.MerchantId,
                    command.PaymentRequestId
                });
                return false;
            }

            return true;
        }        

        private async Task<string> GetMerchantClientIdAsync(CreateSettlementCommand command)
        {
            try
            {
                MerchantModel merchant = await _payMerchantClient.Api.GetByIdAsync(command.MerchantId);
                return merchant.LwId;
            }
            catch (ClientApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                throw new SettlementException(command.MerchantId, command.PaymentRequestId,
                    SettlementProcessingError.MerchantNotFound,
                    $"Merchant {command.MerchantId} is not found.", ex);
            }
        }

        private bool ValidatePaymentRequest(IPaymentRequest paymentRequest)
        {
            if (string.IsNullOrEmpty(paymentRequest.MerchantClientId))
            {
                _log.Info("Skip payment request because merchant Lykke wallet is not set.", new
                {
                    paymentRequest.MerchantId,
                    paymentRequest.PaymentRequestId
                });
                return false;
            }

            return true;
        }

        private bool ValidateMinAmount(IPaymentRequest paymentRequest, out string validateMessage)
        {
            validateMessage = null;
            var paymentAsset = _assetService.GetAsset(paymentRequest.PaymentAssetId);

            if ((decimal)paymentAsset.CashinMinimalAmount > paymentRequest.PaidAmount)
            {
                validateMessage = $"Skip payment request because paid amount ({paymentRequest.PaidAmount}) is less then " +
                          $"{nameof(paymentAsset.CashinMinimalAmount)} ({paymentAsset.CashinMinimalAmount}).";
                return false;
            }            

            if ((decimal?)paymentAsset.LowVolumeAmount >= paymentRequest.PaidAmount)
            {
                validateMessage = $"Skip payment request because paid amount ({paymentRequest.PaidAmount}) is less or equals then " +
                          $"{nameof(paymentAsset.LowVolumeAmount)} ({paymentAsset.LowVolumeAmount}).";
                return false;
            }

            if ((decimal?)paymentAsset.DustLimit >= paymentRequest.PaidAmount)
            {
                validateMessage = $"Skip payment request because paid amount ({paymentRequest.PaidAmount}) is less or equals then " +
                          $"{nameof(paymentAsset.DustLimit)} ({paymentAsset.DustLimit}).";
                return false;
            }

            var settlementAsset = _assetService.GetAsset(paymentRequest.SettlementAssetId);
            if ((decimal)settlementAsset.CashoutMinimalAmount > paymentRequest.Amount)
            {
                validateMessage = $"Skip payment request because amount ({paymentRequest.Amount}) is less then " +
                                  $"{nameof(settlementAsset.CashoutMinimalAmount)} ({settlementAsset.CashoutMinimalAmount}).";
                return false;
            }

            if ((decimal)settlementAsset.CashinMinimalAmount > paymentRequest.Amount)
            {
                validateMessage = $"Skip payment request because amount ({paymentRequest.Amount}) is less then " +
                                  $"{nameof(settlementAsset.CashinMinimalAmount)} ({settlementAsset.CashinMinimalAmount}).";
                return false;
            }

            if ((decimal?)settlementAsset.LowVolumeAmount >= paymentRequest.Amount)
            {
                validateMessage = $"Skip payment request because amount ({paymentRequest.Amount}) is less or equals then " +
                                  $"{nameof(settlementAsset.LowVolumeAmount)} ({settlementAsset.LowVolumeAmount}).";
                return false;
            }

            return true;
        }

        private async Task TryAddPaymentRequestWithErrorAsync(IPaymentRequest paymentRequest, 
            SettlementProcessingError error, IEventPublisher publisher)
        {
            if (paymentRequest == null)
            {
                return;
            }

            paymentRequest.Error = error;

            try
            {
                await AddPaymentRequestAsync(paymentRequest, publisher);
            }
            catch (Exception)
            {
            }
        }

        private async Task AddPaymentRequestAsync(IPaymentRequest paymentRequest,
            IEventPublisher publisher)
        {
            await _paymentRequestService.AddAsync(paymentRequest);

            publisher.PublishEvent(new SettlementCreatedEvent
            {
                PaymentRequestId = paymentRequest.PaymentRequestId,
                MerchantId = paymentRequest.MerchantId,
                IsError = paymentRequest.Error != SettlementProcessingError.None
            });
        }
    }
}
