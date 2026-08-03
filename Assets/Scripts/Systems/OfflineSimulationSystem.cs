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

        /// <summary>
        /// Симулирует интервал (watermark → now), clamp по FixedMaxOfflineSeconds.
        /// При now ≤ watermark — без симуляции.
        /// После догона watermark = now (учтённый wall-time; сверх cap не копится).
        /// GameEvents подавляются на время симуляции.
        /// </summary>
        public OfflineCatchUpResult ApplyCatchUp(ref long maxObservedUnixTimeUtc)
        {
            if (maxObservedUnixTimeUtc < 0)
                maxObservedUnixTimeUtc = 0;

            var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (nowUnix < maxObservedUnixTimeUtc)
            {
                Debug.Log(
                    $"[OfflineSimulation] clock behind watermark " +
                    $"(now={nowUnix}, max={maxObservedUnixTimeUtc}) — skip catch-up");
                LastResult = new OfflineCatchUpResult(
                    applied: false,
                    clockBehindWatermark: true,
                    realElapsedSeconds: 0,
                    simulatedSeconds: 0f,
                    wasCapped: false,
                    resourceDeltas: Array.Empty<OfflineResourceDelta>());
                return LastResult;
            }

            if (nowUnix == maxObservedUnixTimeUtc)
            {
                LastResult = OfflineCatchUpResult.None;
                return LastResult;
            }

            var realElapsed = nowUnix - maxObservedUnixTimeUtc;
            var simulated = Mathf.Min((float)realElapsed, FixedMaxOfflineSeconds);
            var wasCapped = realElapsed > FixedMaxOfflineSeconds;

            var before = SnapshotWallet(_services.Wallet);

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

            var deltas = BuildDeltas(before, _services.Wallet);
            LastResult = new OfflineCatchUpResult(
                applied: true,
                clockBehindWatermark: false,
                realElapsedSeconds: realElapsed,
                simulatedSeconds: simulated,
                wasCapped: wasCapped,
                resourceDeltas: deltas);

            Debug.Log(
                $"[OfflineSimulation] real={realElapsed}s, simulated={simulated:F1}s " +
                $"(cap={FixedMaxOfflineSeconds:F0}s), watermark→{maxObservedUnixTimeUtc}, " +
                $"deltas={deltas.Count}");

            return LastResult;
        }

        private static Dictionary<ResourceDefinition, int> SnapshotWallet(ResourceWallet wallet)
        {
            var snapshot = new Dictionary<ResourceDefinition, int>();
            if (wallet == null)
                return snapshot;

            foreach (var pair in wallet.Amounts)
                snapshot[pair.Key] = pair.Value;

            return snapshot;
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
