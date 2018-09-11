using Lykke.Service.PayInternal.Contract.PaymentRequest;
using System;

namespace Lykke.Service.PaySettlement.Core.Domain
{
    public interface IPaymentRequest : IPaymentRequestIdentifier
    {
        string OrderId { get; }
        decimal Amount { get; }
        string SettlementAssetId { get; }
        string PaymentAssetId { get; }
        DateTime DueDate { get; }
        double MarkupPercent { get; }
        int MarkupPips { get; }
        double MarkupFixedFee { get; }
        string WalletAddress { get; }
        PaymentRequestStatus PaymentRequestStatus { get; }
        decimal PaidAmount { get; }
        DateTime? PaidDate { get; }
        DateTime PaymentRequestTimestamp { get; }
        string TransferToMarketTransactionHash { get; set; }
        decimal TransferToMarketTransactionFee { get; set; }
        SettlementStatus SettlementStatus { get; set; }
        decimal ExchangeAmount { get; set; }
        decimal MarketPrice { get; set; }        
        string MarketOrderId { get; set; }
        decimal TransferredAmount { get; set; }
        string MerchantClientId { get; set; }
        bool Error { get; set; }
        string ErrorDescription { get; set; }
    }
}
