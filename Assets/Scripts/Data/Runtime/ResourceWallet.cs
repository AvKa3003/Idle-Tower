using System.Collections.Generic;
using IdleTower.Data.Definitions;

namespace IdleTower.Data.Runtime
{
    public class ResourceWallet
    {
        private readonly Dictionary<string, int> _amounts = new();

        public IReadOnlyDictionary<string, int> Amounts => _amounts;

        public int GetAmount(ResourceDefinition resource)
        {
            if (resource == null || string.IsNullOrEmpty(resource.Id))
                return 0;

            return _amounts.TryGetValue(resource.Id, out var amount) ? amount : 0;
        }

        public void SetAmount(ResourceDefinition resource, int amount)
        {
            if (resource == null || string.IsNullOrEmpty(resource.Id))
                return;

            _amounts[resource.Id] = amount < 0 ? 0 : amount;
        }

        public void Add(ResourceDefinition resource, int delta)
        {
            if (resource == null || delta == 0)
                return;

            var current = GetAmount(resource);
            SetAmount(resource, current + delta);
        }

        public void Clear()
        {
            _amounts.Clear();
        }
    }
}
