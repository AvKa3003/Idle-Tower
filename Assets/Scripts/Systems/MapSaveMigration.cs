using System;
using System.Collections.Generic;
using IdleTower.Core;
using IdleTower.Data.Definitions;
using IdleTower.Data.Runtime;
using IdleTower.Data.Save;
using IdleTower.Map;
using IdleTower.Map.Behaviors;
using UnityEngine;

namespace IdleTower.Systems
{
    /// <summary>Правила миграции mapCells при Load (этап D).</summary>
    public static class MapSaveMigration
    {
        public static void ApplyCell(
            GameServices services,
            MapCellRuntime runtime,
            MapCellSave save,
            IReadOnlyDictionary<ResourceId, ResourceDefinition> catalog)
        {
            if (services == null || runtime == null || save == null)
                return;

            var behavior = runtime.Definition?.Behavior;
            if (behavior == null)
                return;

            var currentType = behavior.GetBehaviorTypeId();
            var savedType = save.behaviorType ?? string.Empty;

            if (!string.Equals(savedType, currentType, StringComparison.Ordinal))
            {
                MapCellBehaviorEmergency.FinishFromSave(savedType, save, services, catalog);
                runtime.BehaviorState = behavior.CreateDefaultState();
                return;
            }

            MapCellRuntimeState state;
            try
            {
                if (behavior is RaidMapCellBehavior)
                {
                    state = RaidMapCellBehavior.DeserializeStateWithCatalog(
                        save.behaviorStateJson,
                        save,
                        catalog);
                }
                else
                {
                    state = behavior.DeserializeState(save.behaviorStateJson);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[MapSaveMigration] Deserialize failed at ({save.x},{save.y}): {ex.Message}");
                runtime.BehaviorState = behavior.CreateDefaultState();
                return;
            }

            runtime.BehaviorState = state;

            if (behavior is RaidMapCellBehavior && state is RaidMapCellBehaviorState raid)
                MigrateRaidState(services, runtime, raid, save, catalog);
        }

        private static void MigrateRaidState(
            GameServices services,
            MapCellRuntime runtime,
            RaidMapCellBehaviorState state,
            MapCellSave save,
            IReadOnlyDictionary<ResourceId, ResourceDefinition> catalog)
        {
            var site = runtime.RaidSite;
            if (site == null)
                return;

            state.PlannedArmy = ResourceSaveHelper.SanitizePlannedArmy(state.PlannedArmy, catalog);

            if (!state.IsCaptured && state.CompletedRaids >= site.MaxCompletedRaids)
            {
                state.Phase = RaidCellPhase.Captured;
                state.IsPaused = site.PostCaptureMode == PostCaptureMode.RaidFarm;
            }

            // Удалённый/неизвестный PostCaptureMode из сейва → Captured «с нуля» (как сразу после захвата).
            if (!Enum.IsDefined(typeof(PostCaptureMode), save.savedPostCaptureMode))
            {
                if (state.HasActiveRaid)
                    ResourceSaveHelper.GrantRewards(save.activeRaidRewards, services.Resources, catalog);

                RaidMapCellBehavior.ClearActiveRaid(state);
                state.Phase = RaidCellPhase.Captured;
                state.CompletedRaids = Math.Max(state.CompletedRaids, site.MaxCompletedRaids);
                state.IsPaused = site.PostCaptureMode == PostCaptureMode.RaidFarm;
                Debug.LogWarning(
                    $"[MapSaveMigration] ({save.x},{save.y}): неизвестный savedPostCaptureMode=" +
                    $"{save.savedPostCaptureMode} → Captured + default post-state.");
                SanitizePauseForPostCaptureMode(state, site);
                return;
            }

            var savedMode = (PostCaptureMode)save.savedPostCaptureMode;
            var currentMode = site.PostCaptureMode;
            var currentFingerprint = RaidSiteConfigFingerprint.Compute(site, state);
            var savedFingerprint = save.savedConfigFingerprint ?? string.Empty;

            // B: PostCaptureMode изменился + активный рейд (Captured Farm/Passive)
            if (state.HasActiveRaid && state.IsCaptured && savedMode != currentMode)
            {
                ResourceSaveHelper.GrantRewards(save.activeRaidRewards, services.Resources, catalog);
                RaidMapCellBehavior.ClearActiveRaid(state);
                if (currentMode == PostCaptureMode.RaidFarm)
                    state.IsPaused = true;
            }
            // C: PostCaptureMode изменился, активного рейда нет
            else if (!state.HasActiveRaid && savedMode != currentMode)
            {
                if (currentMode == PostCaptureMode.RaidFarm)
                    state.IsPaused = true;
            }
            // A: конфиг изменился, mode тот же
            else if (!string.Equals(savedFingerprint, currentFingerprint, StringComparison.Ordinal))
            {
                // PreCapture — всегда Pause; Captured — только Farm (Dead/Passive без паузы).
                if (!state.IsCaptured || currentMode == PostCaptureMode.RaidFarm)
                    state.IsPaused = true;
            }

            SanitizePauseForPostCaptureMode(state, site);
        }

        /// <summary>Dead/Passive не используют Pause — сбрасываем флаг после Captured.</summary>
        private static void SanitizePauseForPostCaptureMode(
            RaidMapCellBehaviorState state,
            RaidSiteConfig site)
        {
            if (state == null || site == null || !state.IsCaptured)
                return;

            if (site.PostCaptureMode != PostCaptureMode.RaidFarm)
                state.IsPaused = false;
        }
    }
}
