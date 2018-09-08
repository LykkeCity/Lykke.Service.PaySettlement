using AzureStorage.Queue;
using Common;
using Lykke.Service.PaySettlement.Core.Domain;
using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Collections.Generic;
using System.Linq;
using Lykke.Service.PaySettlement.Core.Repositories;

namespace Lykke.Service.PaySettlement.AzureRepositories.TransferToMarket
{
    public class TransferToMarketQueue : ITransferToMarketQueue
    {
        private readonly IQueueExt _queue;
        
        public TransferToMarketQueue(IQueueExt queue)
        {
            _queue = queue;
        }

        public async Task AddPaymentRequestsAsync(IPaymentRequest paymentRequest)
        {
            await _queue.PutRawMessageAsync(new TransferToMarketMessage(paymentRequest).ToJson());
        }

        public async Task<T> ProcessTransferAsync<T>(Func<TransferToMarketMessage[], Task<T>> processor,
            int maxCount = int.MaxValue) where T : IMessageProcessorResult
        {
            var list = new List<Tuple<TransferToMarketMessage, CloudQueueMessage>>();
            CloudQueueMessage rawMessage;
            do
            {
                rawMessage = await _queue.GetRawMessageAsync();
                if (rawMessage != null)
                {
                    list.Add(new Tuple<TransferToMarketMessage, CloudQueueMessage>(
                        rawMessage.AsString.DeserializeJson<TransferToMarketMessage>(), rawMessage));
                }
            } while (rawMessage != null && list.Count < maxCount);

            T result = await processor(list.Select(p=>p.Item1).ToArray());

            if (result.IsSuccess)
            {
                foreach (var message in list)
                {
                    await _queue.FinishRawMessageAsync(message.Item2);
                }
            }

            return result;
        }
    }
}
