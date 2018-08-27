using Lykke.Service.PayInternal.Contract.PaymentRequest;
using System;

namespace Lykke.Service.PaySettlement.Core.Domain
{
    public class PaymentRequest: IPaymentRequest
    {
        public string Id { get; set; }
        public string OrderId { get; set; }
        public string MerchantId { get; set; }
        public Decimal Amount { get; set; }
        public string SettlementAssetId { get; set; }
        public string PaymentAssetId { get; set; }
        public DateTime DueDate { get; set; }
        public double MarkupPercent { get; set; }
        public int MarkupPips { get; set; }
        public double MarkupFixedFee { get; set; }
        public string WalletAddress { get; set; }
        public PaymentRequestStatus Status { get; set; }
        public Decimal PaidAmount { get; set; }
        public DateTime? PaidDate { get; set; }
        public DateTime PaymentRequestTimestamp { get; set; }
        public string TransferToMarketTransactionHash { get; set; }
        public decimal TransferToMarketTransactionFee { get; set; }
        public SettlementStatus SettlementStatus { get; set; }
        public Decimal MarketAmount { get; set; }
        public decimal MarketPrice { get; set; }
        public string MarketOrderId { get; set; }
        public decimal TransferredAmount { get; set; }
        public string MerchantClientId { get; set; }
    }
}
