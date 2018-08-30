using Autofac;
using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.PaySettlement.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Services
{
    public class AssetService: IStartable, IAssetService
    {
        private readonly IAssetsService _assetsService;
        private readonly ILog _log;
        private readonly Core.Settings.AssetServiceSettings _settings;
        private readonly Dictionary<string, AssetPair> _assetPairs;
        private readonly Dictionary<string, Asset> _assets;

        public AssetService(IAssetsService assetsService, ILogFactory logFactory,
            Core.Settings.AssetServiceSettings settings)
        {
            _assetsService = assetsService;
            _log = logFactory.CreateLog(this);
            _settings = settings;
            _assetPairs = new Dictionary<string, AssetPair>();
            _assets = new Dictionary<string, Asset>();
        }

        public void Start()
        {
            var assetGetAllTask = _assetsService.AssetGetAllAsync();
            var assetPairGetAllTask = _assetsService.AssetPairGetAllAsync();
            Task.WhenAll(assetGetAllTask, assetPairGetAllTask).GetAwaiter().GetResult();

            Asset[] assets = assetGetAllTask.Result.ToArray();
            AssetPair[] assetPairs = assetPairGetAllTask.Result.Where(p =>
                    string.Equals(p.BaseAssetId, _settings.PaymentAssetId, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(p.QuotingAssetId, _settings.PaymentAssetId, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (string settlementAssetId in _settings.SettlementAssetIds)
            {
                FillAsset(assets, settlementAssetId);
                FillAssetPair(assetPairs, settlementAssetId);
            }
            FillAsset(assets, _settings.PaymentAssetId);
        }

        private void FillAsset(Asset[] assets, string assetId)
        {
            Asset asset = assets.FirstOrDefault(a =>
                string.Equals(a.Id, assetId, StringComparison.OrdinalIgnoreCase));

            if (asset == null)
            {
                _log.Critical(null, $"Asset {assetId} is not found.");
            }

            _assets[assetId.ToUpper()] = asset;
        }

        private void FillAssetPair(AssetPair[] assetPairs, string settlementAssetId)
        {
            AssetPair assetPair = assetPairs.FirstOrDefault(p =>
                string.Equals(p.BaseAssetId, settlementAssetId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(p.QuotingAssetId, settlementAssetId, StringComparison.OrdinalIgnoreCase));

            if (assetPair == null)
            {
                _log.Critical(null, $"AssetPair for assets {settlementAssetId} " +
                                    $"and {_settings.PaymentAssetId} is not found.");
            }

            _assetPairs[settlementAssetId.ToUpper()] = assetPair;
        }

        public AssetPair GetAssetPair(string paymentAssetId, string settlementAssetId)
        {
            if(!string.Equals(paymentAssetId, _settings.PaymentAssetId, StringComparison.OrdinalIgnoreCase ))
            {
                throw new ArgumentException($"{nameof(paymentAssetId)} ({paymentAssetId}) " +
                                            $"does not equal to required {_settings.PaymentAssetId}.");
            }

            if (!_assetPairs.TryGetValue(settlementAssetId.ToUpper(), out AssetPair assetPair))
            {
                throw new ArgumentException($"{nameof(settlementAssetId)} ({settlementAssetId}) " +
                                            $"is not found in required {_settings.SettlementAssetIds.ToJson()}.");
            }

            return assetPair;
        }

        public Asset GetAsset(string assetId)
        {
            if (!_assets.TryGetValue(assetId.ToUpper(), out Asset asset))
            {
                throw new ArgumentException($"{nameof(assetId)} ({assetId}) is not found.");
            }

            return asset;
        }
    }
}
