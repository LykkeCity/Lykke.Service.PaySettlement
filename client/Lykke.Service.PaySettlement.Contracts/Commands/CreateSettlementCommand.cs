using ProtoBuf;
using System;

namespace Lykke.Service.PaySettlement.Contracts.Commands
{
    [ProtoContract]
    public class CreateSettlementCommand : IPaymentRequestCommand
    {
        [ProtoMember(1, IsRequired = true)]
        public string MerchantId { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public string PaymentRequestId { get; set; }

        [ProtoMember(3)] public string OrderId { get; set; }

        [ProtoMember(4)] public Decimal Amount { get; set; }

        [ProtoMember(5)] public string SettlementAssetId { get; set; }

        [ProtoMember(6)] public string PaymentAssetId { get; set; }

        [ProtoMember(7)] public DateTime DueDate { get; set; }

        [ProtoMember(8)] public double MarkupPercent { get; set; }

        [ProtoMember(9)] public int MarkupPips { get; set; }

        [ProtoMember(10)] public double MarkupFixedFee { get; set; }

        [ProtoMember(11)] public string WalletAddress { get; set; }

        [ProtoMember(12)] public Decimal PaidAmount { get; set; }

        [ProtoMember(13)] public DateTime? PaidDate { get; set; }
    }
}
