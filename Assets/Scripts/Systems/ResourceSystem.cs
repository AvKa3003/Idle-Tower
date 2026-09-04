using System;
using System.Collections.Generic;
using IdleTower.Core;
using IdleTower.Core.Events;
using IdleTower.Data.Definitions;
using IdleTower.Data.Runtime;

namespace IdleTower.Systems
{
    public class ResourceSystem
    {
        private readonly GameServices _services;

        public ResourceSystem(GameServices services)
        {
            _services = services;
        }

        public ResourceWallet Wallet => _services.Wallet;

        /// <summary>Словарь всех ресурсов из GameBalanceConfig (для сейва / миграции).</summary>
        public Dictionary<ResourceId, ResourceDefinition> BuildCatalog()
        {
            var catalog = new Dictionary<ResourceId, ResourceDefinition>();
            var all = _services.Balance?.AllResources;
            if (all == null)
                return catalog;

            for (var i = 0; i < all.Length; i++)
            {
                var resource = all[i];
                if (resource == null || resource.Id.IsEmpty)
                    continue;

                catalog[resource.Id] = resource;
            }

            return catalog;
        }

        public void ApplyStartingResources()
        {
            var balance = _services.Balance;
            if (balance?.StartingResources == null)
                return;

            foreach (var entry in balance.StartingResources)
            {
                if (entry.Amount <= 0)
                    continue;

                EnsureCostResource(entry, "StartingResources");
                Wallet.SetAmount(entry.Resource, entry.Amount);
                GameEvents.RaiseResourceChanged(entry.Resource, entry.Amount);
            }
        }

        public bool CanAfford(ResourceCost[] costs)
        {
            if (costs == null || costs.Length == 0)
                return true;

            foreach (var cost in costs)
            {
                EnsureCostResource(cost, "CanAfford");
                if (Wallet.GetAmount(cost.Resource) < cost.Amount)
                    return false;
            }

            return true;
        }

        public bool TrySpend(ResourceCost[] costs)
        {
            if (costs == null || costs.Length == 0)
                return true;

            if (!CanAfford(costs))
                return false;

            foreach (var cost in costs)
            {
                EnsureCostResource(cost, "TrySpend");
                if (cost.Amount <= 0)
                    continue;

                var newAmount = Wallet.GetAmount(cost.Resource) - cost.Amount;
                Wallet.SetAmount(cost.Resource, newAmount);
                GameEvents.RaiseResourceChanged(cost.Resource, newAmount);
            }

            return true;
        }

        public void Add(ResourceDefinition resource, int amount)
        {
            if (amount == 0)
                return;

            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            Wallet.Add(resource, amount);
            GameEvents.RaiseResourceChanged(resource, Wallet.GetAmount(resource));
        }

        public void Add(ResourceCost[] outputs)
        {
            if (outputs == null)
                return;

            foreach (var output in outputs)
            {
                EnsureCostResource(output, "Add");
                if (output.Amount <= 0)
                    continue;

                Add(output.Resource, output.Amount);
            }
        }

        private static void EnsureCostResource(ResourceCost cost, string context)
        {
            if (cost.Resource == null)
                throw new InvalidOperationException($"[ResourceSystem.{context}] ResourceCost.Resource = null.");
        }
    }
}
