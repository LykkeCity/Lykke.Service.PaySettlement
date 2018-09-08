using ProtoBuf;

namespace Lykke.Service.PaySettlement.Contracts.Events
{
    public class SettlementCreatedEvent
    {
        [ProtoMember(1, IsRequired = true)]
        public string PaymentRequestId { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public string MerchantId { get; set; }

        [ProtoMember(3, IsRequired = true)]
        public bool IsError { get; set; }
    }
}
