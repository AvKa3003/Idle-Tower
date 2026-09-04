using System;
using System.Collections.Generic;
using IdleTower.Data.Definitions;
using IdleTower.Data.Runtime;
using IdleTower.Systems;
using UnityEngine;

namespace IdleTower.Data.Save
{
    public static class ResourceSaveHelper
    {
        public static ResourceAmountSave[] ToSave(ResourceCost[] costs)
        {
            if (costs == null || costs.Length == 0)
                return Array.Empty<ResourceAmountSave>();

            var list = new List<ResourceAmountSave>();
            for (var i = 0; i < costs.Length; i++)
            {
                var cost = costs[i];
                if (cost.Resource == null || cost.Amount <= 0)
                    continue;

                if (cost.Resource.Id.IsEmpty)
                    continue;

                list.Add(new ResourceAmountSave
                {
                    ResourceId = cost.Resource.Id.Value,
                    Amount = cost.Amount
                });
            }

            return list.ToArray();
        }

        public static ResourceCost[] FromSave(
            ResourceAmountSave[] saves,
            IReadOnlyDictionary<ResourceId, ResourceDefinition> catalog)
        {
            if (saves == null || saves.Length == 0 || catalog == null)
                return Array.Empty<ResourceCost>();

            var list = new List<ResourceCost>();
            for (var i = 0; i < saves.Length; i++)
            {
                var entry = saves[i];
                if (string.IsNullOrEmpty(entry.ResourceId) || entry.Amount <= 0)
                    continue;

                var id = ResourceId.FromSerialized(entry.ResourceId);
                if (id.IsEmpty || !catalog.TryGetValue(id, out var resource) || resource == null)
                    continue;

                list.Add(new ResourceCost { Resource = resource, Amount = entry.Amount });
            }

            return list.ToArray();
        }

        public static void GrantRewards(
            ResourceAmountSave[] saves,
            ResourceSystem resources,
            IReadOnlyDictionary<ResourceId, ResourceDefinition> catalog)
        {
            if (resources == null)
                return;

            var costs = FromSave(saves, catalog);
            if (costs.Length > 0)
                resources.Add(costs);
        }

        public static ResourceCost[] SanitizePlannedArmy(
            ResourceCost[] army,
            IReadOnlyDictionary<ResourceId, ResourceDefinition> catalog)
        {
            if (army == null || army.Length == 0 || catalog == null)
                return Array.Empty<ResourceCost>();

            var list = new List<ResourceCost>();
            for (var i = 0; i < army.Length; i++)
            {
                var cost = army[i];
                if (cost.Resource == null || cost.Amount <= 0)
                    continue;

                if (cost.Resource.Id.IsEmpty
                    || !catalog.TryGetValue(cost.Resource.Id, out var resource)
                    || resource == null
                    || !resource.IsUnit)
                {
                    continue;
                }

                list.Add(new ResourceCost { Resource = resource, Amount = cost.Amount });
            }

            return list.ToArray();
        }
    }
}
