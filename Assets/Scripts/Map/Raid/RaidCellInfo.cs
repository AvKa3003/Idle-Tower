using IdleTower.Data.Definitions;
using IdleTower.Map;
using UnityEngine;

namespace IdleTower.Systems
{
    public readonly struct RaidCellInfo
    {
        public Vector2Int Coord { get; }
        public string Title { get; }
        public RaidCellPhase Phase { get; }
        public bool IsPaused { get; }
        public bool HasActiveRaid { get; }
        public int CompletedRaids { get; }
        public int MaxCompletedRaids { get; }
        public float Progress01 { get; }
        public float ElapsedSeconds { get; }
        public float DurationSeconds { get; }
        public ResourceCost[] RequiredUnits { get; }
        public int RequiredStrength { get; }
        public ResourceCost[] PlannedArmy { get; }
        public int PlannedArmyStrength { get; }
        public bool MeetsRequirements { get; }
        public ResourceCost[] Rewards { get; }
        public bool CanStartNow { get; }
        public PostCaptureMode PostCaptureMode { get; }

        public RaidCellInfo(
            Vector2Int coord,
            string title,
            RaidCellPhase phase,
            bool isPaused,
            bool hasActiveRaid,
            int completedRaids,
            int maxCompletedRaids,
            float progress01,
            float elapsedSeconds,
            float durationSeconds,
            ResourceCost[] requiredUnits,
            int requiredStrength,
            ResourceCost[] plannedArmy,
            int plannedArmyStrength,
            bool meetsRequirements,
            ResourceCost[] rewards,
            bool canStartNow,
            PostCaptureMode postCaptureMode)
        {
            Coord = coord;
            Title = title;
            Phase = phase;
            IsPaused = isPaused;
            HasActiveRaid = hasActiveRaid;
            CompletedRaids = completedRaids;
            MaxCompletedRaids = maxCompletedRaids;
            Progress01 = progress01;
            ElapsedSeconds = elapsedSeconds;
            DurationSeconds = durationSeconds;
            RequiredUnits = requiredUnits;
            RequiredStrength = requiredStrength;
            PlannedArmy = plannedArmy;
            PlannedArmyStrength = plannedArmyStrength;
            MeetsRequirements = meetsRequirements;
            Rewards = rewards;
            CanStartNow = canStartNow;
            PostCaptureMode = postCaptureMode;
        }
    }
}
