using Lykke.Service.PayInternal.Contract.PaymentRequest;
using System;

namespace Lykke.Service.PaySettlement.Core.Domain
{
    public interface IPaymentRequest
    {
        string Id { get; }
        string OrderId { get; }
        string MerchantId { get; }
        Decimal Amount { get; }
        string SettlementAssetId { get; }
        string PaymentAssetId { get; }
        DateTime DueDate { get; }
        double MarkupPercent { get; }
        int MarkupPips { get; }
        double MarkupFixedFee { get; }
        string WalletAddress { get; }
        PaymentRequestStatus Status { get; }
        Decimal PaidAmount { get; }
        DateTime? PaidDate { get; }
        DateTime PaymentRequestTimestamp { get; }
        string TransferToMarketTransactionId { get; set; }
        SettlementStatus SettlementStatus { get; set; }
        Decimal MarketAmount { get; set; }
        decimal MarketPrice { get; set; }
        string MarketOrderId { get; set; }
        string MerchantClientId { get; set; }
    }
}
