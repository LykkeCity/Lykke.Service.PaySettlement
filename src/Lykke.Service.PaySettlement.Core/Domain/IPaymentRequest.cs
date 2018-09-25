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
        decimal MarkupPercent { get; }
        int MarkupPips { get; }
        decimal MarkupFixedFee { get; }
        string WalletAddress { get; }
        decimal PaidAmount { get; }
        DateTime? PaidDate { get; }

        DateTime SettlementCreatedUtc { get; }
        SettlementStatus SettlementStatus { get; set; }

        string TransferToMarketTransactionHash { get; set; }
        decimal TransferToMarketTransactionFee { get; set; }
        DateTime? TransferedToMarketUtc { get; }
        
        decimal ExchangeAmount { get; set; }
        decimal MarketPrice { get; set; }        
        string MarketOrderId { get; set; }
        DateTime? ExchangedUtc { get; }

        decimal TransferredAmount { get; set; }
        string MerchantClientId { get; set; }
        DateTime? TransferedToMerchantUtc { get; }

        bool Error { get; set; }
        string ErrorDescription { get; set; }
    }
}
