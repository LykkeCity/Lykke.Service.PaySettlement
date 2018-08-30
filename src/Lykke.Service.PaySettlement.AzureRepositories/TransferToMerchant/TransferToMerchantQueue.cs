using AzureStorage.Queue;
using Common;
using Lykke.Service.PaySettlement.Core.Domain;
using System;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.AzureRepositories.TransferToMerchant
{
    public class TransferToMerchantQueue : ITransferToMerchantQueue
    {
        private readonly IQueueExt _queue;

        public TransferToMerchantQueue(IQueueExt queue)
        {
            _queue = queue;
        }

        public async Task AddPaymentRequestsAsync(IPaymentRequest paymentRequest)
        {
            await _queue.PutRawMessageAsync(new TransferToMerchantMessage(paymentRequest).ToJson());
        }

        public async Task<bool> ProcessTransferAsync(Func<TransferToMerchantMessage, Task<bool>> processor)
        {
            var rawMessage = await _queue.GetRawMessageAsync();
            if (rawMessage == null)
                return false;

            var message = rawMessage.AsString.DeserializeJson<TransferToMerchantMessage>();
            if (await processor(message))
            {
                await _queue.FinishRawMessageAsync(rawMessage);
            }

            return true;
        }
    }
}
