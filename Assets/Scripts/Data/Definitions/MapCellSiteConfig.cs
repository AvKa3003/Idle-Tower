using System;
using IdleTower.Data.Runtime;
using IdleTower.Map;
using UnityEngine;

namespace IdleTower.Data.Definitions
{
    /// <summary>
    /// Per-entry данные клетки на карте (не шаблон Behavior).
    /// Заполняется секция, соответствующая типу Behavior у MapCellDefinition.
    /// </summary>
    [Serializable]
    public class MapCellSiteConfig
    {
        [Tooltip("Если Cell.Behavior = RaidMapCellBehavior.")]
        public RaidSiteConfig Raid;

        // Позже: OneShot, Decorative overrides и т.п.
    }

    /// <summary>Баланс и пост-режим одного рейд-сайта на карте.</summary>
    [Serializable]
    public class RaidSiteConfig
    {
        public RaidConfig PreCapture = new();

        [Min(1)]
        public int MaxCompletedRaids = 1;

        public PostCaptureMode PostCaptureMode = PostCaptureMode.Dead;

        // Видны в Inspector только при соответствующем PostCaptureMode (RaidSiteConfigDrawer).
        public RaidConfig FarmConfig = new();
        public GameDuration PassiveInterval;
        public ResourceCost[] PassiveRewards = Array.Empty<ResourceCost>();

        public Sprite CapturedSprite;

        /// <summary>Активный RaidConfig для текущей фазы. Farm/Passive — позже.</summary>
        public RaidConfig GetActiveRaidConfig(RaidMapCellBehaviorState state)
        {
            if (state == null || !state.IsCaptured)
                return PreCapture;

            // Этап E: PostCaptureMode.RaidFarm → FarmConfig
            // Этап F: Passive — не RaidConfig
            return null;
        }
    }
}
