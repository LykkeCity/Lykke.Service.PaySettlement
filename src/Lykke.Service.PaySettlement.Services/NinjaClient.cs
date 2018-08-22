using Lykke.Service.PaySettlement.Core.Services;
using NBitcoin;
using QBitNinja.Client;
using QBitNinja.Client.Models;
using System;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Services
{
    public class NinjaClient: INinjaClient
    {
        private readonly QBitNinjaClient _client;

        public NinjaClient(QBitNinjaClient client)
        {
            _client = client;
            _client.Colored = true;
        }

        public Task<GetTransactionResponse> GetTransactionAsync(string hash)
        {            
            return _client.GetTransaction(uint256.Parse(hash));
        }

        public Task<GetBlockResponse> GetBlockAsync(int blockHeight)
        {
            return _client.GetBlock(new BlockFeature(blockHeight));
        }

        public async Task<int> GetCurrentBlockNumberAsync()
        {
            return (await _client.GetBlock(new BlockFeature(SpecialFeature.Last), true)).AdditionalInformation.Height;
        }
    }
}
