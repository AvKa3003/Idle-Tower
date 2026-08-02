using System;
using IdleTower.Core;
using IdleTower.Core.Events;
using UnityEngine;

namespace IdleTower.Systems
{
    /// <summary>
    /// Догон тиков за время отсутствия (по watermark UTC в сейве).
    /// Лимит пока фиксированный; позже — от улучшений.
    /// Watermark только растёт: откат системных часов не откатывает прогресс и не даёт
    /// повторный офлайн, пока now не обгонит сохранённый максимум.
    /// </summary>
    public class OfflineSimulationSystem
    {
        /// <summary>Максимум симулируемого отсутствия (сек). Пока константа.</summary>
        public const float FixedMaxOfflineSeconds = 2f * 60f * 60f;

        private readonly GameServices _services;

        public OfflineSimulationSystem(GameServices services)
        {
            _services = services;
        }

        /// <summary>
        /// Симулирует интервал (watermark → now), clamp по FixedMaxOfflineSeconds.
        /// При now ≤ watermark — 0 (откат часов / нет нового времени).
        /// После догона watermark = now (учтённый wall-time; сверх cap не копится).
        /// GameEvents подавляются на время симуляции.
        /// </summary>
        /// <param name="maxObservedUnixTimeUtc">
        /// In/out: максимальный UTC unix из сейва / runtime. Не уменьшается при откате часов.
        /// </param>
        /// <returns>Фактически просимулированные секунды.</returns>
        public float ApplyCatchUp(ref long maxObservedUnixTimeUtc)
        {
            if (maxObservedUnixTimeUtc < 0)
                maxObservedUnixTimeUtc = 0;

            var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (nowUnix < maxObservedUnixTimeUtc)
            {
                Debug.Log(
                    $"[OfflineSimulation] clock behind watermark " +
                    $"(now={nowUnix}, max={maxObservedUnixTimeUtc}) — skip catch-up");
                return 0f;
            }

            if (nowUnix == maxObservedUnixTimeUtc)
                return 0f;

            var realElapsed = nowUnix - maxObservedUnixTimeUtc;
            var simulated = Mathf.Min((float)realElapsed, FixedMaxOfflineSeconds);

            GameEvents.Suppress = true;
            try
            {
                _services.TickSystem.SimulateForSeconds(simulated);
            }
            finally
            {
                GameEvents.Suppress = false;
            }

            // Учтено до now: сверх cap не остаётся «долга» на следующий заход.
            maxObservedUnixTimeUtc = nowUnix;

            Debug.Log(
                $"[OfflineSimulation] real={realElapsed}s, simulated={simulated:F1}s " +
                $"(cap={FixedMaxOfflineSeconds:F0}s), watermark→{maxObservedUnixTimeUtc}");

            return simulated;
        }
    }
}
