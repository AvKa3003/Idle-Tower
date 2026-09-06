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

        /// <summary>
        /// Прогресс текущего таймера: активный рейд (PreCapture/Farm) или цикл Passive.
        /// Одновременно используется только один режим.
        /// </summary>
        public float ElapsedSeconds;

        /// <summary>Состав, который игрок выбрал для повторных набегов.</summary>
        public ResourceCost[] PlannedArmy = Array.Empty<ResourceCost>();

        /// <summary>Награда активного рейда на момент старта (mode-change / emergency).</summary>
        public ResourceCost[] ActiveRaidRewards = Array.Empty<ResourceCost>();

        public bool IsCaptured => Phase == RaidCellPhase.Captured;
    }
}
