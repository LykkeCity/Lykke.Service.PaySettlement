using System;
using ProtoBuf;

namespace Lykke.Service.PaySettlement.Contracts.Commands
{
    [ProtoContract]
    public class ExchangeCommand
    {
        [ProtoMember(1, IsRequired = true)]
        public Guid OperationId { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public string AssetId { get; set; }

        [ProtoMember(3, IsRequired = true)]
        public Decimal Amount { get; set; }

        [ProtoMember(4, IsRequired = true)]
        public Decimal TransactionAmount { get; set; }

        [ProtoMember(5, IsRequired = true)]
        public Decimal TransactionFee { get; set; }

        [ProtoMember(6, IsRequired = true)]
        public string TransactionHash { get; set; }
    }
}
