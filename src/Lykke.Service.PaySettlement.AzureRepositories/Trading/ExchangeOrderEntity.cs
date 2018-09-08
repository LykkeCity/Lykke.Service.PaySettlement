using System;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.MatchingEngine.Connector.Models.Common;
using Lykke.Service.PaySettlement.Core.Domain;

namespace Lykke.Service.PaySettlement.AzureRepositories.Trading
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateIfDirty)]
    public class ExchangeOrderEntity : AzureTableEntity, IExchangeOrder
    {
        public string MerchantId { get; set; }

        public string PaymentRequestId { get; set; }

        public string AssetPairId { get; set; }

        public string PaymentAssetId { get; set; }

        public string SettlementAssetId { get; set; }

        private OrderAction _orderAction;
        public OrderAction OrderAction
        {
            get => _orderAction;
            set
            {
                _orderAction = value;
                MarkValueTypePropertyAsDirty(nameof(OrderAction));
            }
        }

        private decimal _volume;
        public decimal Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                MarkValueTypePropertyAsDirty(nameof(Volume));
            }
        }

        public ExchangeOrderEntity()
        {
        }

        public ExchangeOrderEntity(IExchangeOrder exchangeOrder)
        {
            PartitionKey = GetPartitionKey(exchangeOrder.AssetPairId);
            RowKey = GetRowKey(exchangeOrder.PaymentRequestId);
            MerchantId = exchangeOrder.MerchantId;
            PaymentRequestId = exchangeOrder.PaymentRequestId;
            AssetPairId = exchangeOrder.AssetPairId;
            PaymentAssetId = exchangeOrder.PaymentAssetId;
            SettlementAssetId = exchangeOrder.SettlementAssetId;
            OrderAction = exchangeOrder.OrderAction;
            Volume = exchangeOrder.Volume;
        }

        internal static string GetPartitionKey(string assetPairId)
        {
            if (string.IsNullOrEmpty(assetPairId))
            {
                throw new ArgumentNullException(nameof(assetPairId));
            }

            return assetPairId;
        }

        internal static string GetRowKey(string paymentRequestId)
        {
            if (string.IsNullOrEmpty(paymentRequestId))
            {
                throw new ArgumentNullException(nameof(paymentRequestId));
            }

            return paymentRequestId;
        }
    }
}
