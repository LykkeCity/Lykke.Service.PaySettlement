using Lykke.Service.Assets.Client.Models;
using System;
using Lykke.Service.PaySettlement.Core.Services;

namespace Lykke.Service.PaySettlement.Services
{
    public class AccuracyRoundHelper: IAccuracyRoundHelper
    {
        private readonly IAssetService _assetService;

        public AccuracyRoundHelper(IAssetService assetService)
        {
            _assetService = assetService;
        }

        public double Round(double amount, string assetId)
        {
            Asset asset = _assetService.GetAsset(assetId);
            return Round(amount, asset);
        }

        public double Round(double amount, Asset asset)
        {
            return Round(amount, asset.Accuracy);
        }

        public double Round(double amount, int accuracy)
        {
            double factor = Math.Pow(10, accuracy);
            return Math.Floor(amount * factor) / factor;
        }
    }
}
