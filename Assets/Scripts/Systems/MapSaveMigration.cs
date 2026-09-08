using System;
using System.Collections.Generic;
using IdleTower.Core;
using IdleTower.Data.Definitions;
using IdleTower.Data.Runtime;
using IdleTower.Data.Save;
using IdleTower.Map;
using IdleTower.Map.Behaviors;
using IdleTower.Map.Raid;
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

            // MaxCompletedRaids снизили / сейв «догоняет» Captured — награда за захват из ТЕКУЩЕГО конфига.
            if (!state.IsCaptured && state.CompletedRaids >= site.MaxCompletedRaids)
            {
                if (state.HasActiveRaid)
                {
                    ResourceSaveHelper.GrantRewards(save.activeRaidRewards, services.Resources, catalog);
                    RaidMapCellBehavior.ClearActiveRaid(state);
                }

                state.Phase = RaidCellPhase.Captured;
                state.IsPaused = site.PostCaptureMode == PostCaptureMode.RaidFarm;
                RaidRewardsHelper.GrantCaptureRewards(services, site);
            }

            var currentMode = site.PostCaptureMode;
            var unknownSavedMode = !Enum.IsDefined(typeof(PostCaptureMode), save.savedPostCaptureMode);
            if (unknownSavedMode)
            {
                Debug.LogWarning(
                    $"[MapSaveMigration] ({save.x},{save.y}): неизвестный savedPostCaptureMode=" +
                    $"{save.savedPostCaptureMode} → используем текущий PostCaptureMode из конфига ({currentMode}).");
            }

            // Битый/удалённый mode в сейве ≡ «mode сменился» на актуальный из Site (без принудительного Captured).
            var modeChanged = unknownSavedMode
                || (PostCaptureMode)save.savedPostCaptureMode != currentMode;

            var currentFingerprint = RaidSiteConfigFingerprint.Compute(site, state);
            var savedFingerprint = save.savedConfigFingerprint ?? string.Empty;

            // B: Captured + активный рейд + PostCaptureMode сменился (в т.ч. битый saved)
            if (state.HasActiveRaid && state.IsCaptured && modeChanged)
            {
                ResourceSaveHelper.GrantRewards(save.activeRaidRewards, services.Resources, catalog);
                RaidMapCellBehavior.ClearActiveRaid(state);
                ApplyCapturedPostModeDefaults(state, currentMode);
            }
            // C: mode сменился, активного рейда нет — спец-логика только для уже Captured
            else if (!state.HasActiveRaid && modeChanged && state.IsCaptured)
            {
                ApplyCapturedPostModeDefaults(state, currentMode);
            }
            // A: конфиг изменился, mode тот же (или PreCapture — mode из конфига без спец-веток)
            else if (!modeChanged
                     && !string.Equals(savedFingerprint, currentFingerprint, StringComparison.Ordinal))
            {
                // PreCapture — всегда Pause; Captured — только Farm (Dead/Passive без паузы).
                if (!state.IsCaptured || currentMode == PostCaptureMode.RaidFarm)
                    state.IsPaused = true;
                // Passive: ElapsedSeconds не сбрасываем — цикл догонит/продолжит с новым interval.
                // CaptureRewards / PreCapture Rewards: при активном рейде Complete берёт ТЕКУЩИЙ конфиг
                // (награда за захват никогда не снапшотится в activeRaidRewards).
            }

            SanitizePauseForPostCaptureMode(state, site);
        }

        /// <summary>После смены PostCaptureMode у уже Captured клетки.</summary>
        private static void ApplyCapturedPostModeDefaults(
            RaidMapCellBehaviorState state,
            PostCaptureMode currentMode)
        {
            if (currentMode == PostCaptureMode.RaidFarm)
                state.IsPaused = true;
            if (currentMode == PostCaptureMode.Passive)
                state.ElapsedSeconds = 0f;
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
