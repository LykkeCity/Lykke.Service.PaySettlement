using System;

namespace Lykke.Service.PaySettlement.Core.Settings
{
    public class TransferToMarketServiceSettings : AssetServiceSettings
    {
        public TimeSpan Interval { get; set; }

        public int MaxTransfersPerTransaction { get; set; }

        public string MultisigWalletAddress { get; set; }
    }
}
