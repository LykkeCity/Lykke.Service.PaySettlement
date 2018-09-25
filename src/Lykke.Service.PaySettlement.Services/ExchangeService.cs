﻿using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.MatchingEngine.Connector.Models.Api;
using Lykke.MatchingEngine.Connector.Models.Common;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Core.Services;
using System;
using System.Threading.Tasks;
using Lykke.Service.PaySettlement.Core.Repositories;

namespace Lykke.Service.PaySettlement.Services
{
    public class ExchangeService : IExchangeService
    {
        private readonly IMatchingEngineClient _matchingEngineClient;
        private readonly ITradeOrdersRepository _tradeOrdersRepository;
        private readonly IAssetService _assetService;
        private readonly ILykkeBalanceService _lykkeBalanceService;
        private readonly string _clientId;
        private readonly TimeSpan _attemptInterval;
        private readonly ILog _log;

        public ExchangeService(IMatchingEngineClient matchingEngineClient, 
            ITradeOrdersRepository tradeOrdersRepository, IAssetService assetService, 
            ILykkeBalanceService lykkeBalanceService, string clientId, 
            TimeSpan attemptInterval, ILogFactory logFactory)
        {
            _matchingEngineClient = matchingEngineClient;
            _tradeOrdersRepository = tradeOrdersRepository;
            _assetService = assetService;
            _lykkeBalanceService = lykkeBalanceService;                        
            _clientId = clientId;
            _attemptInterval = attemptInterval;

            _log = logFactory.CreateLog(this);
        }

        public async Task<IExchangeOrder> AddToQueueAsync(IPaymentRequest paymentRequest,
            decimal transferredAmount)
        {
            AssetPair assetPair = _assetService.GetAssetPair(paymentRequest.PaymentAssetId,
                paymentRequest.SettlementAssetId);

            OrderAction orderAction = string.Equals(assetPair.BaseAssetId, paymentRequest.PaymentAssetId,
                StringComparison.OrdinalIgnoreCase)
                ? OrderAction.Sell
                : OrderAction.Buy;

            decimal marketAmount = transferredAmount.TruncateDecimalPlaces(IsStraight(orderAction)
                ? assetPair.InvertedAccuracy
                : assetPair.Accuracy);

            var tradeOrder = new ExchangeOrder()
            {
                MerchantId = paymentRequest.MerchantId,
                PaymentRequestId = paymentRequest.PaymentRequestId,
                Volume = marketAmount,
                AssetPairId = assetPair.Id,
                PaymentAssetId = paymentRequest.PaymentAssetId,
                SettlementAssetId = paymentRequest.SettlementAssetId,
                OrderAction = orderAction,
                LastAttemptUtc = DateTime.UtcNow - _attemptInterval
            };

            await _tradeOrdersRepository.InsertOrReplaceAsync(tradeOrder);
            return tradeOrder;
        }

        public async Task<ExchangeResult> ExchangeAsync()
        {
            IExchangeOrder exchangeOrder =
                await _tradeOrdersRepository.GetTopOrderAsync(DateTime.UtcNow - _attemptInterval);
            if (exchangeOrder == null)
            {
                return null;
            }

            ExchangeResult result = await ExchangeAsync(exchangeOrder);
            if (result.Error == SettlementProcessingError.None || !result.CanBeRetried)
            {
                await _tradeOrdersRepository.DeleteAsync(exchangeOrder);
            }
            else
            {
                await _tradeOrdersRepository.SetLastAttemptAsync(exchangeOrder.AssetPairId,
                    exchangeOrder.PaymentRequestId, DateTime.UtcNow);
            }

            return result;
        }

        private async Task<ExchangeResult> ExchangeAsync(IExchangeOrder exchangeOrder)
        {
            try
            {
                if (!IsBalanceEnough(exchangeOrder, out var isBalanceEnoughResult))
                {
                    return isBalanceEnoughResult;
                }

                MarketOrderResponse response = await HandleMarketOrderAsync(exchangeOrder,
                    out MarketOrderModel model);
                if (!IsResponseSuccess(exchangeOrder, model, response, out var isResponseSuccessResult))
                {
                    return isResponseSuccessResult;
                }

                _lykkeBalanceService.AddAsset(exchangeOrder.PaymentAssetId, -exchangeOrder.Volume);
                _lykkeBalanceService.AddAsset(exchangeOrder.SettlementAssetId, exchangeOrder.Volume * (decimal)response.Price);                

                return new ExchangeResult
                {
                    Error = SettlementProcessingError.None,
                    MerchantId = exchangeOrder.MerchantId,
                    PaymentRequestId = exchangeOrder.PaymentRequestId,
                    MarketPrice = (decimal)response.Price,
                    MarketOrderId = model.Id,
                    AssetPairId = exchangeOrder.AssetPairId
                };
            }
            catch (Exception ex)
            {
                return new ExchangeResult
                {
                    Exception = ex,
                    Error = SettlementProcessingError.Unknown,
                    ErrorMessage = "Unknown error has occured on exchanging.",
                    MerchantId = exchangeOrder.MerchantId,
                    PaymentRequestId = exchangeOrder.PaymentRequestId
                };
            }
        }

        private bool IsBalanceEnough(IExchangeOrder exchangeOrder, out ExchangeResult result)
        {
            result = null;
            decimal balance = _lykkeBalanceService.GetAssetBalance(exchangeOrder.PaymentAssetId);
            if (balance >= exchangeOrder.Volume)
            {
                return true;
            }

            string errorMessage = $"There is not enough balance of {exchangeOrder.PaymentAssetId}. " +
                                  $"Required volume is {exchangeOrder.Volume}. Balance is {balance}.";

            result = new ExchangeResult
            {
                Error = SettlementProcessingError.LowBalanceForExchange,
                ErrorMessage = errorMessage,
                MerchantId = exchangeOrder.MerchantId,
                PaymentRequestId = exchangeOrder.PaymentRequestId
            };
            return false;
        }

        private Task<MarketOrderResponse> HandleMarketOrderAsync(IExchangeOrder exchangeOrder, 
            out MarketOrderModel model)
        {
            model = new MarketOrderModel()
            {
                Id = Guid.NewGuid().ToString(),
            AssetPairId = exchangeOrder.AssetPairId,
                ClientId = _clientId,
                OrderAction = OrderAction.Sell,
                Straight = IsStraight(exchangeOrder.OrderAction),
                Volume = (double)exchangeOrder.Volume
            };

            _log.Info($"Handling market order:\r\n{model.ToJson()}",
                new
                {
                    exchangeOrder.MerchantId,
                    exchangeOrder.PaymentRequestId
                });

            return _matchingEngineClient.HandleMarketOrderAsync(model);
        }

        private bool IsResponseSuccess(IExchangeOrder exchangeOrder, MarketOrderModel model,
            MarketOrderResponse response, out ExchangeResult result)
        {
            result = null;
            if (response?.Status == MeStatusCodes.Ok)
            {
                _log.Info("Handled market order.\r\n " +
                          $"Request: {model.ToJson()}\r\n" +
                          $"Response: {response.ToJson()}", new
                {
                    exchangeOrder.MerchantId,
                    exchangeOrder.PaymentRequestId
                });

                return true;
            }

            string errorMessage = "Can not handle market order.\r\n" +
                                  $"Request: { model.ToJson()}\r\n" +
                                  $"Response: {response?.ToJson()}";

            _log.Warning(errorMessage, null, new
            {
                exchangeOrder.MerchantId,
                exchangeOrder.PaymentRequestId
            });

            result = new ExchangeResult
            {
                ErrorMessage = errorMessage,
                MerchantId = exchangeOrder.MerchantId,
                PaymentRequestId = exchangeOrder.PaymentRequestId
            };

            if (response?.Status == MeStatusCodes.NoLiquidity)
            {
                result.Error = SettlementProcessingError.NoLiquidityForExchange;
            }
            else if (response?.Status == MeStatusCodes.LeadToNegativeSpread)
            {
                result.Error = SettlementProcessingError.ExchangeLeadToNegativeSpread;
            }
            else
            {
                result.Error = SettlementProcessingError.Unknown;
            }

            if (response == null 
                || response?.Status == MeStatusCodes.NoLiquidity
                || response?.Status == MeStatusCodes.LeadToNegativeSpread
                || response?.Status == MeStatusCodes.Runtime)
            {
                result.CanBeRetried = true;                
            }
            else
            {
                result.CanBeRetried = false;                
            }

            return false;
        }

        private bool IsStraight(OrderAction orderAction)
        {
            return orderAction == OrderAction.Sell;
        }
    }
}

