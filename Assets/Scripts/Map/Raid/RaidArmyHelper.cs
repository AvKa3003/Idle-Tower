using System;
using System.Collections.Generic;
using IdleTower.Data.Definitions;
using IdleTower.Data.Runtime;
using IdleTower.Systems;

namespace IdleTower.Map.Raid
{
    /// <summary>Проверка и списание сохранённого состава армии (юниты навсегда).</summary>
    public static class RaidArmyHelper
    {
        public static int CalcStrength(ResourceCost[] costs)
        {
            if (costs == null || costs.Length == 0)
                return 0;

            var total = 0;
            for (var i = 0; i < costs.Length; i++)
            {
                var cost = costs[i];
                if (cost.Resource == null || cost.Amount <= 0)
                    continue;

                if (!cost.Resource.IsUnit)
                    continue;

                total += cost.Amount * Math.Max(0, cost.Resource.Strength);
            }

            return total;
        }

        public static bool MeetsConfigRequirements(RaidConfig config, ResourceCost[] army)
        {
            if (config == null)
                return false;

            var planned = ToAmountMap(army);

            var required = config.RequiredUnits;
            if (required != null)
            {
                for (var i = 0; i < required.Length; i++)
                {
                    var cost = required[i];
                    if (cost.Resource == null || cost.Amount <= 0)
                        continue;

                    if (!cost.Resource.IsUnit)
                        return false;

                    if (!planned.TryGetValue(cost.Resource, out var have) || have < cost.Amount)
                        return false;
                }
            }

            return CalcStrength(army) >= Math.Max(0, config.RequiredStrength);
        }

        public static bool CanAffordArmy(ResourceWallet wallet, ResourceCost[] army)
        {
            if (wallet == null || army == null)
                return false;

            for (var i = 0; i < army.Length; i++)
            {
                var cost = army[i];
                if (cost.Resource == null || cost.Amount <= 0)
                    continue;

                if (wallet.GetAmount(cost.Resource) < cost.Amount)
                    return false;
            }

            return true;
        }

        public static bool TrySpendArmy(ResourceSystem resources, ResourceCost[] army)
        {
            if (resources == null)
                return false;

            return resources.TrySpend(army);
        }

        public static ResourceCost[] CloneNonEmpty(ResourceCost[] army)
        {
            if (army == null || army.Length == 0)
                return Array.Empty<ResourceCost>();

            var list = new List<ResourceCost>(army.Length);
            for (var i = 0; i < army.Length; i++)
            {
                var cost = army[i];
                if (cost.Resource == null || cost.Amount <= 0)
                    continue;

                list.Add(new ResourceCost { Resource = cost.Resource, Amount = cost.Amount });
            }

            return list.Count == 0 ? Array.Empty<ResourceCost>() : list.ToArray();
        }

        public static int GetAmount(ResourceCost[] army, ResourceDefinition resource)
        {
            if (army == null || resource == null)
                return 0;

            for (var i = 0; i < army.Length; i++)
            {
                if (army[i].Resource == resource)
                    return Math.Max(0, army[i].Amount);
            }

            return 0;
        }

        public static ResourceCost[] WithUnitAmount(
            ResourceCost[] army,
            ResourceDefinition resource,
            int amount)
        {
            if (resource == null || !resource.IsUnit)
                return CloneNonEmpty(army);

            var map = ToAmountMap(army);
            if (amount <= 0)
                map.Remove(resource);
            else
                map[resource] = amount;

            if (map.Count == 0)
                return Array.Empty<ResourceCost>();

            var list = new List<ResourceCost>(map.Count);
            foreach (var pair in map)
                list.Add(new ResourceCost { Resource = pair.Key, Amount = pair.Value });

            return list.ToArray();
        }

        private static Dictionary<ResourceDefinition, int> ToAmountMap(ResourceCost[] army)
        {
            var map = new Dictionary<ResourceDefinition, int>();
            if (army == null)
                return map;

            for (var i = 0; i < army.Length; i++)
            {
                var cost = army[i];
                if (cost.Resource == null || cost.Amount <= 0)
                    continue;

                if (!cost.Resource.IsUnit)
                    continue;

                if (map.TryGetValue(cost.Resource, out var current))
                    map[cost.Resource] = current + cost.Amount;
                else
                    map[cost.Resource] = cost.Amount;
            }

            return map;
        }
    }
}
