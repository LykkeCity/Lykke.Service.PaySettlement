using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.Balances.AutorestClient.Models;
using Lykke.Service.Balances.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.PaySettlement.Core.Services;

namespace Lykke.Service.PaySettlement.Services
{
    public class LykkeBalanceService : ILykkeBalanceService
    {
        private readonly IBalancesClient _balancesClient;
        private readonly string _clientId;
        private readonly ConcurrentDictionary<string, decimal> _balances;
        private readonly ILog _log;

        public LykkeBalanceService(IBalancesClient balancesClient, string clientId, ILogFactory logFactory)
        {
            _balancesClient = balancesClient;
            _log = logFactory.CreateLog(this);
            _clientId = clientId;
            _balances = new ConcurrentDictionary<string, decimal>();
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
                    (decimal) model.Balance,
                    (k, v) => (decimal) model.Balance);
            }

            _log.Info("Lykke balances are updated from the server.");
        }

        public void AddAsset(string assetId, decimal value)
        {
            if (_balances.IsEmpty)
            {
                throw new InvalidOperationException($"Call {nameof(GetFromServerAsync)} before change balance.");
            }

            if (!_balances.ContainsKey(assetId))
            {
                throw new ArgumentOutOfRangeException($"Provided assetId {assetId} is not supported by LykkeExchange.");
            }

            decimal newValue = _balances.AddOrUpdate(assetId, value, (k, v) => v + value);

            _log.Info($"Lykkke balance of the {assetId} is updated with {newValue}.");
        }

        public decimal GetAssetBalance(string assetId)
        {
            if (_balances.TryGetValue(assetId, out var balance))
            {
                return balance;
            }

            return 0;
        }
    }
}
