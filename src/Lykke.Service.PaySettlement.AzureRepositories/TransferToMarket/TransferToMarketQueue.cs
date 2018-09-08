using AzureStorage.Queue;
using Common;
using Lykke.Service.PaySettlement.Core.Domain;
using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Collections.Generic;
using System.Linq;

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

        public async Task<int> ProcessTransferAsync(Func<TransferToMarketMessage[], Task<bool>> processor,
            int maxCount = int.MaxValue)
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

           bool result = await processor(list.Select(p=>p.Item1).ToArray());

            if (!result)
            {
                return 0;
            }

            foreach (var message in list)
            {
                await _queue.FinishRawMessageAsync(message.Item2);
            }

            return list.Count;
        }
    }
}
