using System;
using UnityEngine;

namespace IdleTower.Data.Definitions
{
    /// <summary>Параметры одного набега (preCapture / later farm).</summary>
    [Serializable]
    public class RaidConfig
    {
        [Tooltip("Обязательные юниты (списываются при старте).")]
        public ResourceCost[] RequiredUnits = Array.Empty<ResourceCost>();

        [Tooltip("Минимальная суммарная сила армии.")]
        [Min(0)]
        public int RequiredStrength;

        public GameDuration Duration;

        public ResourceCost[] Rewards = Array.Empty<ResourceCost>();
    }
}
