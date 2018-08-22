using Lykke.Service.Assets.Client.Models;

namespace Lykke.Service.PaySettlement.Core.Services
{
    public interface IAssetService
    {
        AssetPair GetAssetPair(string assetPairId);
        AssetPair GetAssetPair(string paymentAssetId, string settlementAssetId);
        Asset GetAsset(string assetId);
    }
}
