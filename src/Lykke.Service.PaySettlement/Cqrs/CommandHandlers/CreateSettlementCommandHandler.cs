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
using Lykke.Service.PaySettlement.Core.Services;
using Lykke.Service.PaySettlement.Cqrs.Helpers;
using Lykke.Service.PaySettlement.Models.Exceptions;
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
            }
            catch (SettlementException ex)
            {
                await TryAddPaymentRequestWithErrorAsync(paymentRequest, ex.Error, ex.Message, publisher);
                await _errorProcessHelper.ProcessErrorAsync(ex, publisher, false);
                return;
            }
            catch (Exception ex)
            {
                string errorMessage = "Unknown error has occured on validating.";
                await TryAddPaymentRequestWithErrorAsync(paymentRequest, SettlementProcessingError.Unknown, 
                    errorMessage, publisher);
                await _errorProcessHelper.ProcessUnknownErrorAsync(command, publisher, false, ex,
                    errorMessage);
                throw;
            }

            try
            {
                await AddPaymentRequestAsync(paymentRequest, publisher);
            }
            catch (Exception ex)
            {
                await _errorProcessHelper.ProcessUnknownErrorAsync(command, publisher, false, ex,
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

            var paymentAsset = _assetService.GetAsset(command.PaymentAssetId);
            if ((decimal)paymentAsset.CashoutMinimalAmount > command.PaidAmount)
            {
                _log.Info($"Skip payment request because paid amount ({command.PaidAmount}) is less then " +
                          $"{nameof(paymentAsset.CashoutMinimalAmount)} ({paymentAsset.CashoutMinimalAmount}).", new
                          {
                              command.MerchantId,
                              command.PaymentRequestId
                          });
                return false;
            }

            if ((decimal)paymentAsset.CashinMinimalAmount > command.PaidAmount)
            {
                _log.Info($"Skip payment request because paid amount ({command.PaidAmount}) is less then " +
                          $"{nameof(paymentAsset.CashinMinimalAmount)} ({paymentAsset.CashinMinimalAmount}).", new
                          {
                              command.MerchantId,
                              command.PaymentRequestId
                          });
                return false;
            }

            if ((decimal?)paymentAsset.LowVolumeAmount >= command.PaidAmount)
            {
                _log.Info($"Skip payment request because paid amount ({command.PaidAmount}) is less or equals then " +
                          $"{nameof(paymentAsset.LowVolumeAmount)} ({paymentAsset.LowVolumeAmount}).", new
                          {
                              command.MerchantId,
                              command.PaymentRequestId
                          });
                return false;
            }

            if ((decimal?)paymentAsset.DustLimit >= command.PaidAmount)
            {
                _log.Info($"Skip payment request because paid amount ({command.PaidAmount}) is less or equals then " +
                          $"{nameof(paymentAsset.DustLimit)} ({paymentAsset.DustLimit}).", new
                          {
                              command.MerchantId,
                              command.PaymentRequestId
                          });
                return false;
            }

            if (null != await _paymentRequestService.GetAsync(command.MerchantId, command.PaymentRequestId))
            {
                _log.Info("Skip payment request because payment request with " +
                          $"{nameof(command.MerchantId)} = {command.MerchantId} " +
                          $"and {nameof(command.PaymentRequestId)} = {command.PaymentRequestId}.", new
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

        private async Task TryAddPaymentRequestWithErrorAsync(IPaymentRequest paymentRequest,
            SettlementProcessingError error, string errorMessage, IEventPublisher publisher)
        {
            if (paymentRequest == null)
            {
                return;
            }

            paymentRequest.Error = error;
            paymentRequest.ErrorDescription = errorMessage;

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
