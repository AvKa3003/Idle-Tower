using System;
using System.Collections.Generic;
using IdleTower.Core;
using IdleTower.Core.Events;
using IdleTower.Data.Definitions;
using IdleTower.Data.Runtime;
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
        public const float FixedMaxOfflineSeconds = 2f * 60f * 60f;

        private readonly GameServices _services;

        public OfflineSimulationSystem(GameServices services)
        {
            _services = services;
            LastResult = OfflineCatchUpResult.None;
        }

        public OfflineCatchUpResult LastResult { get; private set; }

        /// <summary>Снимок кошелька для дельты (вызвать до миграции карты на Load).</summary>
        public static Dictionary<ResourceDefinition, int> SnapshotWallet(ResourceWallet wallet)
        {
            var snapshot = new Dictionary<ResourceDefinition, int>();
            if (wallet == null)
                return snapshot;

            foreach (var pair in wallet.Amounts)
                snapshot[pair.Key] = pair.Value;

            return snapshot;
        }

        /// <summary>
        /// Симулирует интервал (watermark → now), clamp по FixedMaxOfflineSeconds.
        /// При now ≤ watermark — без симуляции; дельта кошелька (миграция карты) всё равно считается.
        /// После догона watermark = now (учтённый wall-time; сверх cap не копится).
        /// При now &lt; watermark watermark не двигаем.
        /// GameEvents подавляются на время симуляции.
        /// <paramref name="walletBaseline"/> — снимок до миграции карты (GrantRewards входят в дельту модалки).
        /// </summary>
        public OfflineCatchUpResult ApplyCatchUp(
            ref long maxObservedUnixTimeUtc,
            Dictionary<ResourceDefinition, int> walletBaseline = null)
        {
            if (maxObservedUnixTimeUtc < 0)
                maxObservedUnixTimeUtc = 0;

            var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var before = walletBaseline ?? SnapshotWallet(_services.Wallet);
            var clockBehind = nowUnix < maxObservedUnixTimeUtc;

            long realElapsed = 0;
            var simulated = 0f;
            var wasCapped = false;

            if (nowUnix > maxObservedUnixTimeUtc)
            {
                realElapsed = nowUnix - maxObservedUnixTimeUtc;
                simulated = Mathf.Min((float)realElapsed, FixedMaxOfflineSeconds);
                wasCapped = realElapsed > FixedMaxOfflineSeconds;

                GameEvents.Suppress = true;
                try
                {
                    _services.TickSystem.SimulateForSeconds(simulated);
                }
                finally
                {
                    GameEvents.Suppress = false;
                }

                maxObservedUnixTimeUtc = nowUnix;
            }
            else if (clockBehind)
            {
                Debug.Log(
                    $"[OfflineSimulation] clock behind watermark " +
                    $"(now={nowUnix}, max={maxObservedUnixTimeUtc}) — skip tick catch-up");
            }

            var deltas = BuildDeltas(before, _services.Wallet);
            var applied = deltas.Count > 0 || simulated > 0f;

            if (!applied && !clockBehind)
            {
                LastResult = OfflineCatchUpResult.None;
                return LastResult;
            }

            LastResult = new OfflineCatchUpResult(
                applied: applied,
                clockBehindWatermark: clockBehind,
                realElapsedSeconds: realElapsed,
                simulatedSeconds: simulated,
                wasCapped: wasCapped,
                resourceDeltas: deltas);

            if (applied)
            {
                Debug.Log(
                    $"[OfflineSimulation] real={realElapsed}s, simulated={simulated:F1}s " +
                    $"(cap={FixedMaxOfflineSeconds:F0}s), clockBehind={clockBehind}, " +
                    $"deltas={deltas.Count}");
            }

            return LastResult;
        }

        private static List<OfflineResourceDelta> BuildDeltas(
            Dictionary<ResourceDefinition, int> before,
            ResourceWallet afterWallet)
        {
            var deltas = new List<OfflineResourceDelta>();
            if (afterWallet == null)
                return deltas;

            var seen = new HashSet<ResourceDefinition>();

            foreach (var pair in afterWallet.Amounts)
            {
                seen.Add(pair.Key);
                before.TryGetValue(pair.Key, out var oldAmount);
                var delta = pair.Value - oldAmount;
                if (delta != 0)
                    deltas.Add(new OfflineResourceDelta(pair.Key, delta));
            }

            foreach (var pair in before)
            {
                if (seen.Contains(pair.Key))
                    continue;

                // Ресурс исчез из кошелька (не ожидается, но на всякий случай).
                if (pair.Value != 0)
                    deltas.Add(new OfflineResourceDelta(pair.Key, -pair.Value));
            }

            deltas.Sort((a, b) =>
            {
                var nameA = a.Resource != null ? a.Resource.DisplayName : string.Empty;
                var nameB = b.Resource != null ? b.Resource.DisplayName : string.Empty;
                return string.CompareOrdinal(nameA, nameB);
            });

            return deltas;
        }
    }
}
