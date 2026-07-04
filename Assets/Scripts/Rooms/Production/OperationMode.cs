using System;
using IdleTower.Data.Definitions;
using UnityEngine;

namespace IdleTower.Rooms.Production
{
    /// <summary>Операция этажа: производство (без входа) или крафт (с InputPerCycle).</summary>
    [Serializable]
    public class OperationMode
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private ResourceCost[] outputPerCycle;
        [SerializeField] private GameDuration cycleDuration;
        [SerializeField] private ResourceCost[] inputPerCycle;
        [SerializeField] private ResourceCost[] unlockCost;
        [SerializeField] private UnlockRule[] unlockRules;
        [SerializeField] private bool unlockedByDefault;

        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public ResourceCost[] OutputPerCycle => outputPerCycle ?? Array.Empty<ResourceCost>();
        public GameDuration CycleDuration => cycleDuration;
        public ResourceCost[] InputPerCycle => inputPerCycle ?? Array.Empty<ResourceCost>();
        public ResourceCost[] UnlockCost => unlockCost ?? Array.Empty<ResourceCost>();
        public UnlockRule[] UnlockRules => unlockRules ?? Array.Empty<UnlockRule>();
        public bool UnlockedByDefault => unlockedByDefault;

        public bool HasCraftInput => InputPerCycle.Length > 0;
        public bool HasOutput => OutputPerCycle.Length > 0;
    }
}
