using Lykke.Service.PayInternal.Contract.PaymentRequest;
using Lykke.Service.PaySettlement.Core.Domain;
using System;

namespace Lykke.Service.PaySettlement.Models
{
    public class PaymentRequestModel
    {
        public string MerchantId { get; set; }
        public string PaymentRequestId { get; set; }
        public string OrderId { get; set; }
        public Decimal Amount { get; set; }
        public string SettlementAssetId { get; set; }
        public string PaymentAssetId { get; set; }
        public DateTime DueDate { get; set; }
        public double MarkupPercent { get; set; }
        public int MarkupPips { get; set; }
        public double MarkupFixedFee { get; set; }
        public string WalletAddress { get; set; }
        public PaymentRequestStatus PaymentRequestStatus { get; set; }
        public Decimal PaidAmount { get; set; }
        public DateTime? PaidDate { get; set; }
        public DateTime PaymentRequestTimestamp { get; set; }

        public DateTime SettlementCreatedUtc { get; set; }
        public SettlementStatus SettlementStatus { get; set; }
        
        public string TransferToMarketTransactionHash { get; set; }
        public decimal TransferToMarketTransactionFee { get; set; }
        public DateTime? TransferedToMarketUtc { get; set; }

        public Decimal ExchangeAmount { get; set; }
        public decimal MarketPrice { get; set; }
        public string MarketOrderId { get; set; }
        public DateTime? ExchangedUtc { get; set; }

        public decimal TransferredAmount { get; set; }
        public string MerchantClientId { get; set; }
        public DateTime? TransferedToMerchantUtc { get; set; }

        public bool Error { get; set; }
        public string ErrorDescription { get; set; }
    }
}
