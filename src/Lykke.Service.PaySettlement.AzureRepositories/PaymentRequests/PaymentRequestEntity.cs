using Lykke.AzureStorage.Tables.Entity.Annotation;
using System;
using System.Collections.Generic;
using System.Text;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.PayInternal.Contract.PaymentRequest;
using Lykke.Service.PaySettlement.Core.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Service.PaySettlement.AzureRepositories.PaymentRequests
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateIfDirty)]
    public class PaymentRequestEntity : AzureTableEntity, IPaymentRequest
    {
        public string PaymentRequestId { get; set; }

        public string OrderId { get; set; }

        public string MerchantId { get; set; }

        private Decimal _amount;
        public Decimal Amount
        {
            get => _amount;
            set
            {
                _amount = value;
                MarkValueTypePropertyAsDirty(nameof(Amount));
            }

        }

        public string SettlementAssetId { get; set; }

        public string PaymentAssetId { get; set; }

        private DateTime _dueDate;
        public DateTime DueDate
        {
            get => _dueDate;
            set
            {
                _dueDate = value;
                MarkValueTypePropertyAsDirty(nameof(DueDate));
            }
        }

        private double _markupPercent;
        public double MarkupPercent
        {
            get => _markupPercent;
            set
            {
                _markupPercent = value;
                MarkValueTypePropertyAsDirty(nameof(MarkupPercent));
            }
        }

        private int _markupPips;
        public int MarkupPips
        {
            get => _markupPips;
            set
            {
                _markupPips = value;
                MarkValueTypePropertyAsDirty(nameof(MarkupPips));
            }
        }

        private double _markupFixedFee;
        public double MarkupFixedFee
        {
            get => _markupFixedFee;
            set
            {
                _markupFixedFee = value;
                MarkValueTypePropertyAsDirty(nameof(MarkupFixedFee));
            }
        }

        public string WalletAddress { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public PaymentRequestStatus PaymentRequestStatus { get; set; }

        private Decimal _paidAmount;
        public Decimal PaidAmount
        {
            get => _paidAmount;
            set
            {
                _paidAmount = value;
                MarkValueTypePropertyAsDirty(nameof(PaidAmount));
            }
        }

        public DateTime? PaidDate { get; set; }

        private DateTime _paymentRequestTimestamp;
        public DateTime PaymentRequestTimestamp
        {
            get => _paymentRequestTimestamp;
            set
            {
                _paymentRequestTimestamp = value;
                MarkValueTypePropertyAsDirty(nameof(PaymentRequestTimestamp));
            }
        }

        public string TransferToMarketTransactionHash { get; set; }

        private decimal _transferToMarketTransactionFee;
        public decimal TransferToMarketTransactionFee {
            get =>_transferToMarketTransactionFee;
            set
            {
                _transferToMarketTransactionFee = value;
                MarkValueTypePropertyAsDirty(nameof(TransferToMarketTransactionFee));
            }
        }

        private SettlementStatus _settlementStatus;
        public SettlementStatus SettlementStatus
        {
            get => _settlementStatus;
            set
            {
                _settlementStatus = value;
                MarkValueTypePropertyAsDirty(nameof(SettlementStatus));
            }
        }

        private decimal _exchangeAmount;
        public decimal ExchangeAmount
        {
            get => _exchangeAmount;
            set
            {
                _exchangeAmount = value;
                MarkValueTypePropertyAsDirty(nameof(ExchangeAmount));
            }
        }

        private decimal _marketPrice;
        public decimal MarketPrice
        {
            get => _marketPrice;
            set
            {
                _marketPrice = value;
                MarkValueTypePropertyAsDirty(nameof(MarketPrice));
            }
        }

        public string MarketOrderId { get; set; }

        private decimal _transferredAmount;
        public decimal TransferredAmount
        {
            get => _transferredAmount;
            set
            {
                _transferredAmount = value;
                MarkValueTypePropertyAsDirty(nameof(TransferredAmount));
            }
        }

        public string MerchantClientId { get; set; }

        private bool _error;
        public bool Error
        {
            get => _error;
            set
            {
                _error = value;
                MarkValueTypePropertyAsDirty(nameof(Error));
            }
        }
        public string ErrorDescription { get; set; }

        public PaymentRequestEntity()
        {
        }

        public PaymentRequestEntity(IPaymentRequest paymentRequest)
        {
            PartitionKey = GetPartitionKey(paymentRequest.MerchantId);
            RowKey = GetRowKey(paymentRequest.PaymentRequestId);
            PaymentRequestId = paymentRequest.PaymentRequestId;
            OrderId = paymentRequest.OrderId;
            MerchantId = paymentRequest.MerchantId;
            Amount = paymentRequest.Amount;
            SettlementAssetId = paymentRequest.SettlementAssetId;
            PaymentAssetId = paymentRequest.PaymentAssetId;
            DueDate = paymentRequest.DueDate;
            MarkupPercent = paymentRequest.MarkupPercent;
            MarkupPips = paymentRequest.MarkupPips;
            MarkupFixedFee = paymentRequest.MarkupFixedFee;
            WalletAddress = paymentRequest.WalletAddress;
            PaymentRequestStatus = paymentRequest.PaymentRequestStatus;
            PaidAmount = paymentRequest.PaidAmount;
            PaidDate = paymentRequest.PaidDate;
            PaymentRequestTimestamp = paymentRequest.PaymentRequestTimestamp;
            TransferToMarketTransactionHash = paymentRequest.TransferToMarketTransactionHash;
            TransferToMarketTransactionFee = paymentRequest.TransferToMarketTransactionFee;
            SettlementStatus = paymentRequest.SettlementStatus;
            ExchangeAmount = paymentRequest.ExchangeAmount;
            MarketPrice = paymentRequest.MarketPrice;
            MarketOrderId = paymentRequest.MarketOrderId;
            TransferredAmount = paymentRequest.TransferredAmount;
            MerchantClientId = paymentRequest.MerchantClientId;
            Error = paymentRequest.Error;
            ErrorDescription = paymentRequest.ErrorDescription;
        }

        internal static string GetPartitionKey(string merchantId)
        {
            if (string.IsNullOrEmpty(merchantId))
            {
                throw new ArgumentNullException(nameof(merchantId));
            }

            return merchantId;
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
