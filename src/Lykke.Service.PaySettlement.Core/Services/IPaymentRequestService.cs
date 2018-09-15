using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.PaySettlement.Core.Domain;

namespace Lykke.Service.PaySettlement.Core.Services
{
    public interface IPaymentRequestService
    {
        Task<IPaymentRequest> GetAsync(IPaymentRequestIdentifier paymentRequestIdentifier);

        Task<IPaymentRequest> GetAsync(string merchantId, string paymentRequestId);

        Task<IPaymentRequest> GetByWalletAddressAsync(string walletAddress);

        Task<IEnumerable<IPaymentRequest>> GetByTransferToMarketTransactionHash(string transactionHash);

        Task<(IEnumerable<IPaymentRequest> Entities, string ContinuationToken)> GetBySettlementCreatedAsync(
            DateTime from, DateTime to, int take, string continuationToken = null);

        Task<(IEnumerable<IPaymentRequest> Entities, string ContinuationToken)> GetByMerchantAsync(
            string merchantId, int take, string continuationToken = null);

        Task AddAsync(IPaymentRequest paymentRequest);

        Task<IPaymentRequest> SetTransferToMarketQueuedAsync(string merchantId, string id);

        Task<IPaymentRequest> SetTransferringToMarketAsync(string merchantId, string id, 
            string transactionHash);        

        Task<IPaymentRequest> SetExchangeQueuedAsync(IExchangeOrder exchangeOrder, decimal transactionFee);

        Task<IPaymentRequest> SetExchangedAsync(string merchantId, string id, decimal marketPrice, 
            string marketOrderId);

        Task<IPaymentRequest> SetTransferredToMerchantAsync(string merchantId, string id, 
            decimal transferredAmount);

        Task<IPaymentRequest> SetErrorAsync(string merchantId, string id, string errorDescription);
    }
}
