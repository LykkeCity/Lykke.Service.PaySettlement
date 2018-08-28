using Lykke.Service.Assets.Client.Models;

namespace Lykke.Service.PaySettlement.Core.Services
{
    public interface IAccuracyRoundHelper
    {
        double Round(double amount, string assetId);
        double Round(double amount, Asset asset);
        double Round(double amount, int accuracy);
    }
}
