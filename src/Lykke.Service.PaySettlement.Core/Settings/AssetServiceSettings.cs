﻿using System;

namespace Lykke.Service.PaySettlement.Core.Settings
{
    public class AssetServiceSettings
    {
        public string PaymentAssetId { get; set; }

        public string[] SettlementAssetIds { get; set; }

        public TimeSpan ExpirationPeriod { get; set; }
    }
}
