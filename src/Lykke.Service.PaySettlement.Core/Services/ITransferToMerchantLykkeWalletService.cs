using Lykke.Service.PaySettlement.Core.Domain;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Core.Services
{
    public interface ITransferToMerchantLykkeWalletService
    {
        Task<TransferToMerchantResult> TransferAsync(IPaymentRequest message);
    }
}
