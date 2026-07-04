using UnityEngine;

namespace IdleTower.Data.Definitions
{
    [CreateAssetMenu(fileName = "GameBalance", menuName = "IdleTower/Game Balance Config")]
    public class GameBalanceConfig : ScriptableObject
    {
        [Header("Tick")]
        [Min(1)] [SerializeField] private int ticksPerSecond = 10;
        [Min(0.1f)] [SerializeField] private float maxCatchUpSeconds = 2f;

        [Header("Resources")]
        [SerializeField] private ResourceDefinition[] allResources;
        [SerializeField] private ResourceCost[] startingResources;

        public int TicksPerSecond => ticksPerSecond;
        public float MaxCatchUpSeconds => maxCatchUpSeconds;
        public ResourceDefinition[] AllResources => allResources;
        public ResourceCost[] StartingResources => startingResources;

        public int MaxTicksPerFrame => Mathf.Max(1, ticksPerSecond * Mathf.CeilToInt(maxCatchUpSeconds));
        public float TickInterval => 1f / Mathf.Max(1, ticksPerSecond);
    }
}
