using QBitNinja.Client.Models;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Core.Services
{
    public interface INinjaClient
    {
        Task<GetTransactionResponse> GetTransactionAsync(string hash);

        Task<GetBlockResponse> GetBlockAsync(int blockHeight);

        Task<int> GetCurrentBlockNumberAsync();
    }
}
