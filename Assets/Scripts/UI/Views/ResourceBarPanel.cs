using System;
using System.Collections.Generic;
using IdleTower.Data.Definitions;
using UnityEngine;

namespace IdleTower.UI.Views
{
    public class ResourceBarPanel : MonoBehaviour
    {
        [SerializeField] private Transform slotsRoot;
        [SerializeField] private ResourceSlotView slotPrefab;

        private readonly List<ResourceSlotView> _spawnedSlots = new();

        public void SetResources(IReadOnlyList<ResourceDefinition> resources, Func<ResourceDefinition, int> amountProvider)
        {
            ClearSlots();

            if (resources == null || slotPrefab == null)
                return;

            var parent = slotsRoot != null ? slotsRoot : transform;

            for (var i = 0; i < resources.Count; i++)
            {
                var resource = resources[i];
                if (resource == null)
                    continue;

                var slot = Instantiate(slotPrefab, parent);
                var amount = amountProvider != null ? amountProvider(resource) : 0;
                slot.SetResource(resource, amount);
                _spawnedSlots.Add(slot);
            }
        }

        public void RefreshAmounts(IReadOnlyList<ResourceDefinition> resources, Func<ResourceDefinition, int> amountProvider)
        {
            if (resources == null || amountProvider == null)
                return;

            for (var i = 0; i < resources.Count && i < _spawnedSlots.Count; i++)
            {
                var resource = resources[i];
                if (resource == null)
                    continue;

                _spawnedSlots[i].SetResource(resource, amountProvider(resource));
            }
        }

        private void ClearSlots()
        {
            for (var i = 0; i < _spawnedSlots.Count; i++)
            {
                if (_spawnedSlots[i] != null)
                    Destroy(_spawnedSlots[i].gameObject);
            }

            _spawnedSlots.Clear();
        }
    }
}
