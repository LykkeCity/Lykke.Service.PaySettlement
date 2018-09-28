using ProtoBuf;

namespace Lykke.Service.PaySettlement.Contracts.Events
{
    [ProtoContract]
    public class SettlementTransferringToMarketEvent
    {
        [ProtoMember(1, IsRequired = true)]
        public string PaymentRequestId { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public string MerchantId { get; set; }
        
        [ProtoMember(3, IsRequired = true)]
        public string TransactionHash { get; set; }

        [ProtoMember(4, IsRequired = true)]
        public string DestinationAddress { get; set; }

        [ProtoMember(5, IsRequired = true)]
        public decimal Amount { get; set; }

        [ProtoMember(6, IsRequired = true)]
        public string AssetId { get; set; }
    }
}
