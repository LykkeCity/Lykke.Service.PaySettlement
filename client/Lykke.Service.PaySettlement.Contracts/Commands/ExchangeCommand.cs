using ProtoBuf;

namespace Lykke.Service.PaySettlement.Contracts.Commands
{
    [ProtoContract]
    public class ExchangeCommand
    {
        [ProtoMember(1, IsRequired = true)]
        public string TransactionHash { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public decimal TransactionFee { get; set; }
    }
}
