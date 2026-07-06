using System;
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

        public void ApplyStartingResources()
        {
            var balance = _services.Balance;
            if (balance?.StartingResources == null)
                return;

            foreach (var entry in balance.StartingResources)
            {
                if (entry.Resource == null || entry.Amount <= 0)
                    continue;

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
                if (cost.Resource == null)
                    continue;

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
                if (cost.Resource == null || cost.Amount <= 0)
                    continue;

                var newAmount = Wallet.GetAmount(cost.Resource) - cost.Amount;
                Wallet.SetAmount(cost.Resource, newAmount);
                GameEvents.RaiseResourceChanged(cost.Resource, newAmount);
            }

            return true;
        }

        public void Add(ResourceDefinition resource, int amount)
        {
            if (resource == null || amount == 0)
                return;

            Wallet.Add(resource, amount);
            GameEvents.RaiseResourceChanged(resource, Wallet.GetAmount(resource));
        }

        public void Add(ResourceCost[] outputs)
        {
            if (outputs == null)
                return;

            foreach (var output in outputs)
            {
                if (output.Resource == null || output.Amount <= 0)
                    continue;

                Add(output.Resource, output.Amount);
            }
        }
    }
}
