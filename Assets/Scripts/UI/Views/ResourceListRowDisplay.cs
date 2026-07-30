using IdleTower.Data.Definitions;
using UnityEngine;

namespace IdleTower.UI.Views
{
    public readonly struct ResourceListRowDisplay
    {
        public ResourceDefinition Resource { get; }
        public string Name { get; }
        public Sprite Icon { get; }
        public int Amount { get; }

        public ResourceListRowDisplay(ResourceDefinition resource, string name, Sprite icon, int amount)
        {
            Resource = resource;
            Name = name;
            Icon = icon;
            Amount = amount;
        }
    }
}
