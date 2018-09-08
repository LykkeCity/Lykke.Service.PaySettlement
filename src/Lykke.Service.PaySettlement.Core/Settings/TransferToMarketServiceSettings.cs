using System;

namespace Lykke.Service.PaySettlement.Core.Settings
{
    public class TransferToMarketServiceSettings
    {
        public int MaxTransfersPerTransaction { get; set; }

        public string MultisigWalletAddress { get; set; }
    }
}
