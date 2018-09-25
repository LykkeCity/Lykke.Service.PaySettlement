using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Core.Services
{
    public interface ILykkeBalanceService
    {
        Task GetFromServerAsync();
        void AddAsset(string assetId, decimal value);
        decimal GetAssetBalance(string assetId);
    }
}
