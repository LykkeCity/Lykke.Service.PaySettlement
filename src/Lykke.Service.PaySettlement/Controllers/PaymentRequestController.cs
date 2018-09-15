using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Common.Log;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Common.Log;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Core.Services;
using Lykke.Service.PaySettlement.Models;
using LykkePay.Common.Validation;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lykke.Service.PaySettlement.Controllers
{
    [ValidateModel]
    [Route("api/[controller]/[action]")]
    public class PaymentRequestController : Controller
    {
        private readonly IPaymentRequestService _paymentRequestService;
        private readonly IMapper _mapper;
        private readonly ILog _log;

        public PaymentRequestController(IPaymentRequestService paymentRequestService, 
            IMapper mapper, ILogFactory logFactory)
        {
            _paymentRequestService = paymentRequestService;
            _mapper = mapper;
            _log = logFactory.CreateLog(this);
        }

        /// <summary>
        /// Returns payment request.
        /// </summary>
        /// <param name="merchantId">Identifier of the merchant.</param>
        /// <param name="paymentRequestId">Identifier of the payment request.</param>
        /// <returns code="200">Payment request.</returns>
        /// <returns code="404">Payment request is not found.</returns>
        /// <returns code="400">Input arguments are invalid.</returns>
        [HttpGet]
        [SwaggerOperation("GetPaymentRequest")]
        [ProducesResponseType(typeof(PaymentRequestModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [ValidateModel]
        public async Task<IActionResult> GetPaymentRequest([Required, RowKey]string merchantId, 
            [Required, RowKey]string paymentRequestId)
        {
            IPaymentRequest paymentRequest = await _paymentRequestService.GetAsync(merchantId, paymentRequestId);

            if (paymentRequest == null)
            {
                return NotFound();
            }

            var model = _mapper.Map<PaymentRequestModel>(paymentRequest);
            return Ok(model);
        }

        /// <summary>
        /// Returns payment request.
        /// </summary>
        /// <param name="walletAddress">Payment request's wallet address.</param>
        /// <returns code="200">Payment request.</returns>
        /// <returns code="404">Payment request is not found.</returns>
        /// <returns code="400">Input arguments are invalid.</returns>
        [HttpGet]
        [SwaggerOperation("GetPaymentRequestByWalletAddress")]
        [ProducesResponseType(typeof(PaymentRequestModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [ValidateModel]
        public async Task<IActionResult> GetPaymentRequestByWalletAddress([Required, RowKey]string walletAddress)
        {
            IPaymentRequest paymentRequest = await _paymentRequestService.GetByWalletAddressAsync(walletAddress);

            if (paymentRequest == null)
            {
                return NotFound();
            }

            var model = _mapper.Map<PaymentRequestModel>(paymentRequest);
            return Ok(model);
        }

        /// <summary>
        /// Returns payment requests.
        /// </summary>
        /// <param name="transactionHash">Payment request's transfer to market transaction hash.</param>
        /// <returns code="200">Payment requests.</returns>
        /// <returns code="404">Payment requests are not found.</returns>
        /// <returns code="400">Input arguments are invalid.</returns>
        [HttpGet]
        [SwaggerOperation("GetPaymentRequestsByTransactionHash")]
        [ProducesResponseType(typeof(IEnumerable<PaymentRequestModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [ValidateModel]
        public async Task<IActionResult> GetPaymentRequestsByTransactionHash(
            [Required, RowKey]string transactionHash)
        {
            IEnumerable<IPaymentRequest> paymentRequests = await _paymentRequestService.GetByTransferToMarketTransactionHash(transactionHash);

            if (!paymentRequests.Any())
            {
                return NotFound();
            }

            var models = _mapper.Map<IEnumerable<PaymentRequestModel>>(paymentRequests);
            return Ok(models);
        }

        /// <summary>
        /// Returns payment requests.
        /// </summary>
        /// <param name="from">Bottom border of the settlement start interval.</param>
        /// <param name="to">Top border of the settlement start interval.</param>
        /// <param name="take">Max count of entries in the result.</param>
        /// <param name="continuationToken">Token for next page, pass null for first page.</param>
        /// <returns code="200">Payment requests.</returns>
        /// <returns code="404">Payment requests are not found.</returns>
        /// <returns code="400">Input arguments are invalid.</returns>
        [HttpGet]
        [SwaggerOperation("GetPaymentRequestsBySettlementCreated")]
        [ProducesResponseType(typeof(ContinuationResult<PaymentRequestModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [ValidateModel]
        public async Task<IActionResult> GetPaymentRequestsBySettlementCreated(
            DateTime from, DateTime to, int take, string continuationToken = null)
        {
            var paymentRequests = await _paymentRequestService.GetBySettlementCreatedAsync(
                from, to, take, continuationToken);

            if (!paymentRequests.Entities.Any())
            {
                return NotFound();
            }

            var models = _mapper.Map<IEnumerable<PaymentRequestModel>>(paymentRequests.Entities);
            return Ok(new ContinuationResult<PaymentRequestModel>()
            {
                Entities = models,
                ContinuationToken = paymentRequests.ContinuationToken
            });
        }

        /// <summary>
        /// Returns payment requests.
        /// </summary>
        /// <param name="merchantId">Identifier of the merchant.</param>
        /// <param name="take">Max count of entries in the result.</param>
        /// <param name="continuationToken">Token for next page, pass null for first page.</param>
        /// <returns code="200">Payment requests.</returns>
        /// <returns code="404">Payment requests are not found.</returns>
        /// <returns code="400">Input arguments are invalid.</returns>
        [HttpGet]
        [SwaggerOperation("GetPaymentRequestsByMerchant")]
        [ProducesResponseType(typeof(ContinuationResult<PaymentRequestModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [ValidateModel]
        public async Task<IActionResult> GetPaymentRequestsByMerchant(
            string merchantId, int take, string continuationToken = null)
        {
            var paymentRequests = await _paymentRequestService.GetByMerchantAsync(
                merchantId, take, continuationToken);

            if (!paymentRequests.Entities.Any())
            {
                return NotFound();
            }

            var models = _mapper.Map<IEnumerable<PaymentRequestModel>>(paymentRequests.Entities);
            return Ok(new ContinuationResult<PaymentRequestModel>()
            {
                Entities = models,
                ContinuationToken = paymentRequests.ContinuationToken
            });
        }
    }
}
