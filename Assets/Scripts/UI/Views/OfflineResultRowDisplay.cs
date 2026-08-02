using IdleTower.Data.Definitions;
using UnityEngine;

namespace IdleTower.UI.Views
{
    /// <summary>DTO строки офлайн-отчёта: ресурс и дельта кошелька.</summary>
    public readonly struct OfflineResultRowDisplay
    {
        public ResourceDefinition Resource { get; }
        public string Name { get; }
        public Sprite Icon { get; }
        public int Delta { get; }

        public OfflineResultRowDisplay(ResourceDefinition resource, string name, Sprite icon, int delta)
        {
            Resource = resource;
            Name = name;
            Icon = icon;
            Delta = delta;
        }
    }
}
