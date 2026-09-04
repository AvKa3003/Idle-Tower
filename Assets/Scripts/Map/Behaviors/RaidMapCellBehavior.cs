using System.Collections.Generic;
using System;
using IdleTower.Core;
using IdleTower.Data.Definitions;
using IdleTower.Data.Runtime;
using IdleTower.Data.Save;
using IdleTower.Map;
using IdleTower.Map.Raid;
using UnityEngine;

namespace IdleTower.Map.Behaviors
{
    [CreateAssetMenu(fileName = "RaidMapCell", menuName = "IdleTower/Map Cell Behavior/Raid")]
    public class RaidMapCellBehavior : MapCellBehaviorBase
    {
        /// <summary>До захвата не expander; после — через ShouldRevealNeighbors(runtime).</summary>
        public override bool RevealsNeighborsWhenInteractive => false;

        public override string GetBehaviorTypeId() => MapCellBehaviorIds.Raid;

        public override bool ShouldRevealNeighbors(MapCellRuntime runtime)
            => runtime?.BehaviorState is RaidMapCellBehaviorState state && state.IsCaptured;

        public override MapCellClickResult OnClicked(MapCellBehaviorContext context)
            => new(MapCellClickAction.OpenRaid);

        public override MapCellRuntimeState CreateDefaultState()
            => new RaidMapCellBehaviorState();

        public override Sprite GetDisplaySprite(MapCellRuntime runtime)
        {
            if (runtime?.BehaviorState is RaidMapCellBehaviorState state
                && state.IsCaptured
                && runtime.RaidSite?.CapturedSprite != null)
            {
                return runtime.RaidSite.CapturedSprite;
            }

            return null;
        }

        public override string SerializeState(MapCellRuntimeState state)
        {
            if (state is not RaidMapCellBehaviorState raid)
                return string.Empty;

            var dto = RaidMapCellStateDto.FromState(raid);
            return JsonUtility.ToJson(dto);
        }

        public override MapCellRuntimeState DeserializeState(string json)
        {
            if (string.IsNullOrEmpty(json))
                return CreateDefaultState();

            try
            {
                var dto = JsonUtility.FromJson<RaidMapCellStateDto>(json);
                return dto?.ToState() ?? CreateDefaultState();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RaidMapCellBehavior] Deserialize failed: {ex.Message}");
                return CreateDefaultState();
            }
        }

        public static void EmergencyFinishFromSave(
            MapCellSave save,
            GameServices services,
            IReadOnlyDictionary<ResourceId, ResourceDefinition> catalog)
        {
            if (save == null || services == null || catalog == null)
                return;

            if (string.IsNullOrEmpty(save.behaviorStateJson))
                return;

            var dto = JsonUtility.FromJson<RaidMapCellStateDto>(save.behaviorStateJson);
            if (dto == null || !dto.hasActiveRaid)
                return;

            ResourceSaveHelper.GrantRewards(save.activeRaidRewards, services.Resources, catalog);
        }

        internal static RaidMapCellBehaviorState DeserializeStateWithCatalog(
            string json,
            MapCellSave save,
            IReadOnlyDictionary<ResourceId, ResourceDefinition> catalog)
        {
            if (string.IsNullOrEmpty(json))
                return new RaidMapCellBehaviorState();

            try
            {
                var dto = JsonUtility.FromJson<RaidMapCellStateDto>(json);
                var state = dto?.ToState() ?? new RaidMapCellBehaviorState();

                if (dto != null && catalog != null)
                    state.PlannedArmy = ResourceSaveHelper.FromSave(dto.plannedArmy, catalog);

                if (state.HasActiveRaid && save != null && catalog != null)
                {
                    state.ActiveRaidRewards = ResourceSaveHelper.FromSave(
                        save.activeRaidRewards,
                        catalog);
                }

                return state;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RaidMapCellBehavior] DeserializeWithCatalog failed: {ex.Message}");
                return new RaidMapCellBehaviorState();
            }
        }

        internal static void ClearActiveRaid(RaidMapCellBehaviorState state)
        {
            if (state == null)
                return;

            state.HasActiveRaid = false;
            state.ActiveElapsedSeconds = 0f;
            state.ActiveRaidRewards = Array.Empty<ResourceCost>();
        }

        [Serializable]
        internal sealed class RaidMapCellStateDto
        {
            public int phase;
            public bool isPaused;
            public int completedRaids;
            public bool hasActiveRaid;
            public float activeElapsedSeconds;
            public ResourceAmountSave[] plannedArmy = Array.Empty<ResourceAmountSave>();

            public static RaidMapCellStateDto FromState(RaidMapCellBehaviorState state)
            {
                return new RaidMapCellStateDto
                {
                    phase = (int)state.Phase,
                    isPaused = state.IsPaused,
                    completedRaids = state.CompletedRaids,
                    hasActiveRaid = state.HasActiveRaid,
                    activeElapsedSeconds = state.ActiveElapsedSeconds,
                    plannedArmy = ResourceSaveHelper.ToSave(state.PlannedArmy)
                };
            }

            public RaidMapCellBehaviorState ToState()
            {
                return new RaidMapCellBehaviorState
                {
                    Phase = Enum.IsDefined(typeof(RaidCellPhase), phase)
                        ? (RaidCellPhase)phase
                        : RaidCellPhase.PreCapture,
                    IsPaused = isPaused,
                    CompletedRaids = Math.Max(0, completedRaids),
                    HasActiveRaid = hasActiveRaid,
                    ActiveElapsedSeconds = Math.Max(0f, activeElapsedSeconds),
                    PlannedArmy = Array.Empty<ResourceCost>()
                };
            }
        }
    }
}
