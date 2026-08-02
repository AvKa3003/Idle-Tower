using System;
using System.Collections.Generic;
using IdleTower.Data.Definitions;

namespace IdleTower.Systems
{
    /// <summary>Изменение одного ресурса за офлайн-догон (после − до).</summary>
    public readonly struct OfflineResourceDelta
    {
        public ResourceDefinition Resource { get; }
        public int Delta { get; }

        public OfflineResourceDelta(ResourceDefinition resource, int delta)
        {
            Resource = resource;
            Delta = delta;
        }
    }

    /// <summary>Итог OfflineSimulationSystem.ApplyCatchUp (для UI / логов).</summary>
    public sealed class OfflineCatchUpResult
    {
        public static OfflineCatchUpResult None { get; } = new OfflineCatchUpResult(
            applied: false,
            clockBehindWatermark: false,
            realElapsedSeconds: 0,
            simulatedSeconds: 0f,
            wasCapped: false,
            resourceDeltas: Array.Empty<OfflineResourceDelta>());

        public bool Applied { get; }
        public bool ClockBehindWatermark { get; }
        public long RealElapsedSeconds { get; }
        public float SimulatedSeconds { get; }
        public bool WasCapped { get; }
        public IReadOnlyList<OfflineResourceDelta> ResourceDeltas { get; }

        public OfflineCatchUpResult(
            bool applied,
            bool clockBehindWatermark,
            long realElapsedSeconds,
            float simulatedSeconds,
            bool wasCapped,
            IReadOnlyList<OfflineResourceDelta> resourceDeltas)
        {
            Applied = applied;
            ClockBehindWatermark = clockBehindWatermark;
            RealElapsedSeconds = realElapsedSeconds;
            SimulatedSeconds = simulatedSeconds;
            WasCapped = wasCapped;
            ResourceDeltas = resourceDeltas ?? Array.Empty<OfflineResourceDelta>();
        }
    }
}
