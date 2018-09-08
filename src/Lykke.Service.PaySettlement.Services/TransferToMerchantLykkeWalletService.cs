﻿using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.MatchingEngine.Connector.Models.Api;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.ExchangeOperations.Client;
using Lykke.Service.ExchangeOperations.Client.AutorestClient.Models;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Core.Services;
using System;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Services
{
    public class TransferToMerchantLykkeWalletService : ITransferToMerchantLykkeWalletService
    {
        private readonly IAssetService _assetService;
        private readonly ILykkeBalanceService _lykkeBalanceService;
        private readonly IExchangeOperationsServiceClient _exchangeOperationsServiceClient;
        private readonly string _clientId;
        private readonly ILog _log;

        public TransferToMerchantLykkeWalletService(IAssetService assetService, 
            ILykkeBalanceService lykkeBalanceService, 
            IExchangeOperationsServiceClient exchangeOperationsServiceClient,
            string clientId, ILogFactory logFactory)
        {
            _assetService = assetService;
            _lykkeBalanceService = lykkeBalanceService;
            _exchangeOperationsServiceClient = exchangeOperationsServiceClient;
            _clientId = clientId;

            _log = logFactory.CreateLog(this);
        }

        public async Task<TransferToMerchantResult> TransferAsync(IPaymentRequest message)
        {
            try
            {
                if (!IsBalanceEnough(message, out var isBalanceEnoughResult))
                {
                    return isBalanceEnoughResult;
                }

                decimal amount = GetAmount(message);
                ExchangeOperationResult result = await TransferExAsync(message, amount);

                if (!IsExchangeOperationSuccess(message, result, out var isExchangeOperationSuccessResult))
                {
                    return isExchangeOperationSuccessResult;
                }

                _lykkeBalanceService.AddAsset(message.SettlementAssetId, -amount);                

                return new TransferToMerchantResult
                {
                    IsSuccess = true,
                    MerchantId = message.MerchantId,
                    PaymentRequestId = message.PaymentRequestId,
                    Amount = amount,
                    AssetId = message.SettlementAssetId
                };
            }
            catch (Exception ex)
            {
                string errorMessage = "Transfer to merchant is failed.";

                _log.Error(ex, errorMessage, new
                {
                    message.MerchantId,
                    message.PaymentRequestId
                });

                return new TransferToMerchantResult
                {
                    IsSuccess = false,
                    ErrorMessage = errorMessage,
                    MerchantId = message.MerchantId,
                    PaymentRequestId = message.PaymentRequestId
                };
            }
        }

        private bool IsBalanceEnough(IPaymentRequest message, out TransferToMerchantResult result)
        {
            result = null;
            decimal balance = _lykkeBalanceService.GetAssetBalance(message.SettlementAssetId);

            if (balance >= message.Amount)
            {
                return true;
            }

            string errorMessage = $"There is not enought balance of {message.SettlementAssetId}. " +
                                  $"Required volume is {message.Amount}. Balance is {balance}.";

            _log.Error(null, errorMessage, new
            {
                message.MerchantId,
                message.PaymentRequestId
            });

            result = new TransferToMerchantResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                MerchantId = message.MerchantId,
                PaymentRequestId = message.PaymentRequestId
            };
            return false;
        }

        private decimal GetAmount(IPaymentRequest message)
        {
            Asset asset = _assetService.GetAsset(message.SettlementAssetId);
            return message.Amount.TruncateDecimalPlaces(asset.Accuracy);
        }

        private Task<ExchangeOperationResult> TransferExAsync(IPaymentRequest message, 
            decimal amount)
        {
            var request = new
            {
                destClientId = message.MerchantClientId,
                sourceClientId = _clientId,
                amount = amount,
                assetId = message.SettlementAssetId
            };

            _log.Info($"Transferring to merchant request:\r\n{request.ToJson()}",
                new
                {
                    message.MerchantId,
                    message.PaymentRequestId
                });

            return _exchangeOperationsServiceClient.TransferAsync(
                request.destClientId, request.sourceClientId, (double)request.amount, request.assetId);
        }

        private bool IsExchangeOperationSuccess(IPaymentRequest message, 
            ExchangeOperationResult exchangeOperationResult, out TransferToMerchantResult result)
        {
            result = null;
            if (exchangeOperationResult?.Code == (int)MeStatusCodes.Ok)
            {
                _log.Info($"Transfer to merchant operation is success.\r\nResult: {exchangeOperationResult.ToJson()}", new
                {
                    message.MerchantId,
                    message.PaymentRequestId
                });

                return true;
            }

            string errorMessage = $"Can not transfer to merchant.\r\nResponse: {exchangeOperationResult?.ToJson()}";

            _log.Error(null, errorMessage, new
            {
                message.MerchantId,
                message.PaymentRequestId
            });

            result = new TransferToMerchantResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                MerchantId = message.MerchantId,
                PaymentRequestId = message.PaymentRequestId
            };

            return false;
        }
    }
}
