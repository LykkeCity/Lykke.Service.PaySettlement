using ProtoBuf;

namespace Lykke.Service.PaySettlement.Contracts.Commands
{
    [ProtoContract]
    public class TransferToMerchantCommand : IPaymentRequestCommand
    {
        [ProtoMember(1, IsRequired = true)]
        public string MerchantId { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public string PaymentRequestId { get; set; }
    }
}
