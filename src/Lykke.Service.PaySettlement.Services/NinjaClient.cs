﻿using Lykke.Service.PaySettlement.Core.Services;
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
    }
}
