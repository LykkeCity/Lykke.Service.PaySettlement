using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Service.PaySettlement.Models;
using Refit;

namespace Lykke.Service.PaySettlement.Client
{
    /// <summary>
    /// PaySettlement client API interface.
    /// </summary>
    [PublicAPI]
    public interface IPaySettlementApi
    {
        /// <summary>
        /// Returns payment request.
        /// </summary>
        /// <param name="merchantId">Identifier of the merchant.</param>
        /// <param name="paymentRequestId">Identifier of the payment request.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
        /// <returns code="200">Payment request.</returns>
        /// <returns code="404">Payment request is not found.</returns>
        /// <returns code="400">Input arguments are invalid.</returns>
        [Get("/api/PaymentRequest/GetPaymentRequest/")]
        Task<PaymentRequestModel> GetPaymentRequestAsync(string merchantId,
            string paymentRequestId, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns payment request.
        /// </summary>
        /// <param name="walletAddress">Payment request's wallet address.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
        /// <returns code="200">Payment request.</returns>
        /// <returns code="404">Payment request is not found.</returns>
        /// <returns code="400">Input arguments are invalid.</returns>
        [Get("/api/PaymentRequest/GetPaymentRequestByWalletAddress/")]
        Task<PaymentRequestModel> GetPaymentRequestByWalletAddressAsync(string walletAddress, 
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns payment requests.
        /// </summary>
        /// <param name="transactionHash">Payment request's transfer to market transaction hash.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
        /// <returns code="200">Payment requests.</returns>
        /// <returns code="404">Payment requests are not found.</returns>
        /// <returns code="400">Input arguments are invalid.</returns>
        [Get("/api/PaymentRequest/GetPaymentRequestsByTransactionHash/")]
        Task<IEnumerable<PaymentRequestModel>> GetPaymentRequestsByTransactionHashAsync(string transactionHash, 
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns payment requests.
        /// </summary>
        /// <param name="from">Bottom border of the settlement start interval.</param>
        /// <param name="to">Top border of the settlement start interval.</param>
        /// <param name="take">Max count of entries in the result.</param>
        /// <param name="continuationToken">Token for next page, pass null for first page.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
        /// <returns code="200">Payment requests.</returns>
        /// <returns code="404">Payment requests are not found.</returns>
        /// <returns code="400">Input arguments are invalid.</returns>
        [Get("/api/PaymentRequest/GetPaymentRequestsBySettlementCreated/")]
        Task<ContinuationResult<PaymentRequestModel>> GetPaymentRequestsBySettlementCreatedAsync(
            DateTime from, DateTime to, int take, string continuationToken = null,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns payment requests.
        /// </summary>
        /// <param name="merchantId">Identifier of the merchant.</param>
        /// <param name="take">Max count of entries in the result.</param>
        /// <param name="continuationToken">Token for next page, pass null for first page.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
        /// <returns code="200">Payment requests.</returns>
        /// <returns code="404">Payment requests are not found.</returns>
        /// <returns code="400">Input arguments are invalid.</returns>
        [Get("/api/PaymentRequest/GetPaymentRequestsByMerchant/")]
        Task<ContinuationResult<PaymentRequestModel>> GetPaymentRequestsByMerchantAsync(string merchantId, 
            int take, string continuationToken = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}
