using System;
using Lykke.Service.PaySettlement.Core.Domain;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Core.Repositories
{
    public interface ITradeOrdersRepository
    {
        Task InsertOrReplaceAsync(IExchangeOrder exchangeOrder);

        Task<IExchangeOrder> GetTopOrderAsync(DateTime lastAttemptUtc);

        Task<IExchangeOrder> SetLastAttemptAsync(string assetPairId, string paymentRequestId,
            DateTime lastAttemptUtc);

        Task DeleteAsync(IExchangeOrder exchangeOrder);
    }
}
