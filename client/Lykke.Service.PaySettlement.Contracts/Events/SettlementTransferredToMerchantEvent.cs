﻿using ProtoBuf;

namespace Lykke.Service.PaySettlement.Contracts.Events
{
    [ProtoContract]
    public class SettlementTransferredToMerchantEvent
    {
        [ProtoMember(1, IsRequired = true)]
        public string PaymentRequestId { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public string MerchantId { get; set; }

        [ProtoMember(3, IsRequired = true)]
        public decimal TransferredAmount { get; set; }

        [ProtoMember(4, IsRequired = true)]
        public string TransferredAssetId { get; set; }
    }
}
