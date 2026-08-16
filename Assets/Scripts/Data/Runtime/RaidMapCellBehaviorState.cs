using System;
using IdleTower.Data.Definitions;
using IdleTower.Map;

namespace IdleTower.Data.Runtime
{
    public sealed class RaidMapCellBehaviorState : MapCellRuntimeState
    {
        public RaidCellPhase Phase = RaidCellPhase.PreCapture;
        public bool IsPaused;
        public int CompletedRaids;
        public bool HasActiveRaid;
        public float ActiveElapsedSeconds;

        /// <summary>Состав, который игрок выбрал для повторных набегов.</summary>
        public ResourceCost[] PlannedArmy = Array.Empty<ResourceCost>();

        public bool IsCaptured => Phase == RaidCellPhase.Captured;
    }
}
