using System;

namespace Lykke.Service.PaySettlement.Core.Settings
{
    public class TransferToMarketServiceSettings
    {
        public TimeSpan Interval { get; set; }

        public int MaxTransfersPerTransaction { get; set; }

        public string MultisigWalletAddress { get; set; }
    }
}
