using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.Balances.AutorestClient.Models;
using Lykke.Service.Balances.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Lykke.Service.PaySettlement.Core.Services;

namespace Lykke.Service.PaySettlement.Services
{
    public class LykkeBalanceService : TimerPeriod, ILykkeBalanceService
    {
        private readonly IBalancesClient _balancesClient;
        private readonly string _clientId;
        private readonly ConcurrentDictionary<string, decimal> _balances;
        private readonly ILog _log;
        private volatile bool _receivedFromServer = false;

        public LykkeBalanceService(IBalancesClient balancesClient, string clientId,
            TimeSpan interval, ILogFactory logFactory) : base(interval, logFactory)
        {
            _balancesClient = balancesClient;
            _log = logFactory.CreateLog(this);
            _clientId = clientId;
            _balances = new ConcurrentDictionary<string, decimal>();
        }

        public override Task Execute()
        {
            return GetFromServerAsync();
        }

        public async Task GetFromServerAsync()
        {
            IEnumerable<ClientBalanceResponseModel> response = await _balancesClient.GetClientBalances(_clientId);

            if (response == null)
            {
                return;
            }

            _balances.Clear();

            foreach (ClientBalanceResponseModel model in response)
            {
                _balances.AddOrUpdate(model.AssetId,
                    model.Balance,
                    (k, v) => v + model.Balance);
            }

            _receivedFromServer = true;
        }

        public void AddAsset(string assetId, decimal value)
        {
            if (!_receivedFromServer)
            {
                throw new InvalidOperationException($"Call {nameof(GetFromServerAsync)} before change balance.");
            }

            decimal newValue = _balances.AddOrUpdate(assetId, value, (k, v) => v + value);

            _log.Info($"Lykkke balance of the {assetId} is updated with {newValue}.");
        }

        public decimal GetAssetBalance(string assetId)
        {
            if (!_receivedFromServer)
            {
                throw new InvalidOperationException($"Call {nameof(GetFromServerAsync)} before get balance.");
            }

            if (_balances.TryGetValue(assetId, out var balance))
            {
                return balance;
            }

            return 0;
        }
    }
}
