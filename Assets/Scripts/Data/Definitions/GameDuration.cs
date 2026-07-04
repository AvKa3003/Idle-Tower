using System;
using UnityEngine;

namespace IdleTower.Data.Definitions
{
    [Serializable]
    public struct GameDuration
    {
        [Min(0)] public int Minutes;
        [Min(0)] public int Seconds;

        public float TotalSeconds => Minutes * 60f + Seconds;

        public static GameDuration FromSeconds(float seconds)
        {
            var total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return new GameDuration
            {
                Minutes = total / 60,
                Seconds = total % 60
            };
        }
    }
}
