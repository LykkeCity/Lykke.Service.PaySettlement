using ProtoBuf;

namespace Lykke.Service.PaySettlement.Contracts.Events
{
    [ProtoContract]
    public class SettlementTransferToMarketQueuedEvent
    {
        [ProtoMember(1, IsRequired = true)]
        public string PaymentRequestId { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public string MerchantId { get; set; }
    }
}
