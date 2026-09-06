using System;
using System.Collections.Generic;
using IdleTower.Core;
using IdleTower.Core.Events;
using IdleTower.Data.Definitions;
using IdleTower.Data.Runtime;
using IdleTower.Data.Save;
using IdleTower.Map;
using IdleTower.Map.Behaviors;
using IdleTower.Map.Raid;
using UnityEngine;

namespace IdleTower.Systems
{
    /// <summary>
    /// Карта: presence, клики, тик рейдов (прогресс + автостарт), pause/capture.
    /// </summary>
    public sealed class MapSystem : ITickable
    {
        private static readonly Vector2Int[] FourOffsets =
        {
            new(0, 1), new(0, -1), new(-1, 0), new(1, 0)
        };

        private static readonly Vector2Int[] NineOffsets =
        {
            new(-1, 1), new(0, 1), new(1, 1),
            new(-1, 0), new(1, 0),
            new(-1, -1), new(0, -1), new(1, -1)
        };

        private readonly GameServices _services;
        private readonly List<Vector2Int> _autostartBuffer = new();

        public MapState State { get; } = new();

        public MapConfig Config => _services.MapConfig;

        public MapSystem(GameServices services)
        {
            _services = services;
            ReloadFromConfig();
        }

        public void ReloadFromConfig()
        {
            State.LoadFromConfig(Config);
            RecomputeMapPresence();
        }

        /// <summary>Накладывает mapCells из сейва после LoadFromConfig.</summary>
        public void ApplySaveFromDisk(
            MapCellSave[] saves,
            IReadOnlyDictionary<ResourceId, ResourceDefinition> catalog)
        {
            if (saves == null || saves.Length == 0 || catalog == null)
                return;

            var saveByCoord = new Dictionary<Vector2Int, MapCellSave>();
            for (var i = 0; i < saves.Length; i++)
            {
                var save = saves[i];
                saveByCoord[new Vector2Int(save.x, save.y)] = save;
            }

            foreach (var pair in State.Cells)
            {
                if (!saveByCoord.TryGetValue(pair.Key, out var save))
                    continue;

                MapSaveMigration.ApplyCell(_services, pair.Value, save, catalog);
                saveByCoord.Remove(pair.Key);
            }

            // Coord убран из MapConfig — emergency-награда за активный рейд, state не восстанавливаем.
            foreach (var orphan in saveByCoord)
            {
                MapCellBehaviorEmergency.FinishFromSave(
                    orphan.Value.behaviorType,
                    orphan.Value,
                    _services,
                    catalog);
            }

            RecomputeMapPresence();
        }

        /// <summary>Устанавливаем статусы видимости и доступности клеток.</summary>
        public void RecomputeMapPresence()
        {
            var config = Config;
            if (config == null || State.Cells.Count == 0)
                return;

            var expanders = BuildExpanderSet();
            // Собираем дистанции от всех клеток расширителей
            var distances = ComputeDistancesFrom(expanders);

            var interaction = Mathf.Max(0, config.InteractionRadius);
            var vision = Mathf.Max(interaction, config.VisionRadius);

            // Устанавливаем статусы видимости и интерактивности по расстоянию до клетки от расширителей
            foreach (var pair in State.Cells)
            {
                if (!distances.TryGetValue(pair.Key, out var distance))
                {
                    pair.Value.Presence = MapPresence.Fog;
                    continue;
                }

                if (distance <= interaction)
                    pair.Value.Presence = MapPresence.Interactive;
                else if (distance <= vision)
                    pair.Value.Presence = MapPresence.VisibleOnly;
                else
                    pair.Value.Presence = MapPresence.Fog;
            }

            GameEvents.RaiseMapPresenceChanged();
        }

        public void OnTick(TickContext context)
        {
            var capturedChanged = AdvanceActiveRaids(context.TickDelta);
            AdvancePassiveIncome(context.TickDelta);
            if (capturedChanged)
                RecomputeMapPresence();

            TryAutostartRaids();
        }

        public MapCellClickResult TryClick(Vector2Int coord)
        {
            if (!State.TryGet(coord, out var runtime))
                return MapCellClickResult.None;

            if (runtime.Presence != MapPresence.Interactive)
                return MapCellClickResult.None;

            var behavior = runtime.Definition?.Behavior;
            if (behavior == null)
                return MapCellClickResult.None;

            var ctx = new MapCellBehaviorContext(coord, runtime.Definition, runtime, _services);
            return behavior.OnClicked(ctx);
        }

        public bool TryTogglePause(Vector2Int coord)
        {
            if (!TryGetRaid(coord, out _, out var state, out var site))
                return false;

            if (!AllowsPlayPause(state, site))
                return false;

            // Play всегда доступен (даже без юнитов в кошельке) — только снимает паузу.
            state.IsPaused = !state.IsPaused;
            return true;
        }

        /// <summary>Меняет число юнита в сохранённом составе; любое изменение → Pause.</summary>
        public bool TrySetPlannedArmyUnit(Vector2Int coord, ResourceDefinition unit, int amount)
        {
            if (unit == null || !unit.IsUnit)
                return false;

            if (!TryGetRaid(coord, out _, out var state, out var site))
                return false;

            if (!AllowsPlayPause(state, site))
                return false;

            var next = Math.Max(0, amount);
            state.PlannedArmy = RaidArmyHelper.WithUnitAmount(state.PlannedArmy, unit, next);
            state.IsPaused = true;
            return true;
        }

        public bool TryStartRaidIfPossible(Vector2Int coord)
        {
            if (!TryGetRaid(coord, out _, out var state, out var site))
                return false;

            if (!CanStartRaid(coord, state, site, out var config, out var army))
                return false;

            if (!RaidArmyHelper.TrySpendArmy(_services.Resources, army))
                return false;

            state.ActiveRaidRewards = RaidArmyHelper.CloneNonEmpty(config?.Rewards ?? Array.Empty<ResourceCost>());
            state.HasActiveRaid = true;
            state.ElapsedSeconds = 0f;
            return true;
        }

        public bool TryGetRaidInfo(Vector2Int coord, out RaidCellInfo info)
        {
            info = default;
            if (!TryGetRaid(coord, out _, out var state, out var site))
                return false;

            if (state.IsCaptured
                && site != null
                && site.PostCaptureMode == PostCaptureMode.Passive)
            {
                var passiveDuration = Mathf.Max(0f, site.PassiveInterval.TotalSeconds);
                var passiveElapsed = state.ElapsedSeconds;
                var passiveProgress = passiveDuration > 0f
                    ? Mathf.Clamp01(passiveElapsed / passiveDuration)
                    : 0f;

                info = new RaidCellInfo(
                    coord,
                    runtimeTitle(coord),
                    state.Phase,
                    isPaused: false,
                    hasActiveRaid: false,
                    state.CompletedRaids,
                    site.MaxCompletedRaids,
                    passiveProgress,
                    passiveElapsed,
                    passiveDuration,
                    Array.Empty<ResourceCost>(),
                    requiredStrength: 0,
                    Array.Empty<ResourceCost>(),
                    plannedArmyStrength: 0,
                    meetsRequirements: true,
                    site.PassiveRewards ?? Array.Empty<ResourceCost>(),
                    canStartNow: false,
                    PostCaptureMode.Passive);
                return true;
            }

            var config = site != null ? site.GetActiveRaidConfig(state) : null;
            if (config == null && !state.IsCaptured && site != null)
                config = site.PreCapture;

            var duration = config != null ? Mathf.Max(0f, config.Duration.TotalSeconds) : 0f;
            var elapsed = state.HasActiveRaid ? state.ElapsedSeconds : 0f;
            var progress = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 0f;

            var planned = RaidArmyHelper.CloneNonEmpty(state.PlannedArmy);
            var plannedStrength = RaidArmyHelper.CalcStrength(planned);
            var meets = config != null && RaidArmyHelper.MeetsConfigRequirements(config, planned);
            var canStart = CanStartRaid(coord, state, site, out _, out _);

            info = new RaidCellInfo(
                coord,
                runtimeTitle(coord),
                state.Phase,
                state.IsPaused,
                state.HasActiveRaid,
                state.CompletedRaids,
                site != null ? site.MaxCompletedRaids : 1,
                progress,
                elapsed,
                duration,
                config?.RequiredUnits ?? Array.Empty<ResourceCost>(),
                config?.RequiredStrength ?? 0,
                planned,
                plannedStrength,
                meets,
                config?.Rewards ?? Array.Empty<ResourceCost>(),
                canStart,
                site != null ? site.PostCaptureMode : PostCaptureMode.Dead);
            return true;

            string runtimeTitle(Vector2Int c)
            {
                if (State.TryGet(c, out var rt) && rt.Definition != null
                    && !string.IsNullOrWhiteSpace(rt.Definition.DisplayName))
                {
                    return rt.Definition.DisplayName;
                }

                return "Набег";
            }
        }

        public bool TryGetCellDisplay(Vector2Int coord, out MapCellDisplayInfo info)
        {
            info = default;
            if (!State.TryGet(coord, out var runtime) || runtime.Definition == null)
                return false;

            info = BuildDisplay(coord, runtime);
            return true;
        }

        public IEnumerable<MapCellDisplayInfo> EnumerateDisplays()
        {
            foreach (var pair in State.Cells)
            {
                var runtime = pair.Value;
                if (runtime?.Definition == null)
                    continue;

                yield return BuildDisplay(pair.Key, runtime);
            }
        }

        private MapCellDisplayInfo BuildDisplay(Vector2Int coord, MapCellRuntime runtime)
        {
            var behavior = runtime.Definition.Behavior;
            var overrideSprite = behavior != null ? behavior.GetDisplaySprite(runtime) : null;
            var sprite = overrideSprite != null ? overrideSprite : runtime.Definition.Sprite;

            return new MapCellDisplayInfo(
                coord,
                runtime.Definition,
                runtime.Presence,
                HasFunctionalClick(runtime),
                CanAcceptClick(runtime),
                sprite);
        }

        private bool AdvanceActiveRaids(float tickDelta)
        {
            var presenceDirty = false;

            foreach (var pair in State.Cells)
            {
                if (!TryGetRaidRuntime(pair.Value, out _, out var state, out var site))
                    continue;

                if (!state.HasActiveRaid)
                    continue;

                var config = site != null ? site.GetActiveRaidConfig(state) : null;
                if (config == null)
                {
                    state.HasActiveRaid = false;
                    state.ElapsedSeconds = 0f;
                    state.ActiveRaidRewards = Array.Empty<ResourceCost>();
                    continue;
                }

                var duration = Mathf.Max(0.0001f, config.Duration.TotalSeconds);
                state.ElapsedSeconds += tickDelta;

                if (state.ElapsedSeconds < duration)
                    continue;

                CompleteRaid(pair.Key, state, site, config, ref presenceDirty);
            }

            return presenceDirty;
        }

        private void CompleteRaid(
            Vector2Int coord,
            RaidMapCellBehaviorState state,
            RaidSiteConfig site,
            RaidConfig config,
            ref bool presenceDirty)
        {
            state.HasActiveRaid = false;
            state.ElapsedSeconds = 0f;
            state.ActiveRaidRewards = Array.Empty<ResourceCost>();

            if (config.Rewards != null && config.Rewards.Length > 0)
                _services.Resources.Add(config.Rewards);

            if (state.IsCaptured)
                return;

            state.CompletedRaids++;
            var maxRaids = site != null ? site.MaxCompletedRaids : 1;
            if (state.CompletedRaids < maxRaids)
                return;

            state.Phase = RaidCellPhase.Captured;
            // Farm иначе сразу автостартует и может съесть армию/ресурсы без согласия игрока.
            state.IsPaused = site != null && site.PostCaptureMode == PostCaptureMode.RaidFarm;
            presenceDirty = true;
        }

        private void AdvancePassiveIncome(float tickDelta)
        {
            foreach (var pair in State.Cells)
            {
                if (pair.Value.Presence != MapPresence.Interactive)
                    continue;

                if (!TryGetRaidRuntime(pair.Value, out _, out var state, out var site))
                    continue;

                if (!state.IsCaptured || site == null)
                    continue;

                if (site.PostCaptureMode != PostCaptureMode.Passive)
                    continue;

                var interval = site.PassiveInterval.TotalSeconds;
                if (interval <= 0f)
                    continue;

                state.ElapsedSeconds += tickDelta;

                while (state.ElapsedSeconds >= interval)
                {
                    state.ElapsedSeconds -= interval;
                    if (site.PassiveRewards != null && site.PassiveRewards.Length > 0)
                        _services.Resources.Add(site.PassiveRewards);
                }
            }
        }

        private void TryAutostartRaids()
        {
            _autostartBuffer.Clear();

            foreach (var pair in State.Cells)
            {
                if (pair.Value.Presence != MapPresence.Interactive)
                    continue;

                if (!TryGetRaidRuntime(pair.Value, out _, out var state, out var site))
                    continue;

                if (!CanStartRaid(pair.Key, state, site, out _, out _))
                    continue;

                _autostartBuffer.Add(pair.Key);
            }

            _autostartBuffer.Sort(CompareCoordYThenX);

            for (var i = 0; i < _autostartBuffer.Count; i++)
                TryStartRaidIfPossible(_autostartBuffer[i]);
        }

        private static int CompareCoordYThenX(Vector2Int a, Vector2Int b)
        {
            var byY = a.y.CompareTo(b.y);
            return byY != 0 ? byY : a.x.CompareTo(b.x);
        }

        private bool CanStartRaid(
            Vector2Int coord,
            RaidMapCellBehaviorState state,
            RaidSiteConfig site,
            out RaidConfig config,
            out ResourceCost[] army)
        {
            config = null;
            army = Array.Empty<ResourceCost>();

            if (site == null)
                return false;

            if (!State.TryGet(coord, out var runtime))
                return false;

            if (runtime.Presence != MapPresence.Interactive)
                return false;

            if (state.IsPaused || state.HasActiveRaid)
                return false;

            config = site.GetActiveRaidConfig(state);
            if (config == null)
                return false;

            if (!state.IsCaptured && state.CompletedRaids >= site.MaxCompletedRaids)
                return false;

            army = RaidArmyHelper.CloneNonEmpty(state.PlannedArmy);
            if (!RaidArmyHelper.MeetsConfigRequirements(config, army))
                return false;

            return RaidArmyHelper.CanAffordArmy(_services.Wallet, army);
        }

        private static bool AllowsPlayPause(RaidMapCellBehaviorState state, RaidSiteConfig site)
        {
            if (state == null)
                return false;

            if (!state.IsCaptured)
                return true;

            return site != null && site.PostCaptureMode == PostCaptureMode.RaidFarm;
        }

        private bool TryGetRaid(
            Vector2Int coord,
            out RaidMapCellBehavior behavior,
            out RaidMapCellBehaviorState state,
            out RaidSiteConfig site)
        {
            behavior = null;
            state = null;
            site = null;

            if (!State.TryGet(coord, out var runtime))
                return false;

            return TryGetRaidRuntime(runtime, out behavior, out state, out site);
        }

        private static bool TryGetRaidRuntime(
            MapCellRuntime runtime,
            out RaidMapCellBehavior behavior,
            out RaidMapCellBehaviorState state,
            out RaidSiteConfig site)
        {
            behavior = runtime?.Definition?.Behavior as RaidMapCellBehavior;
            state = runtime?.BehaviorState as RaidMapCellBehaviorState;
            site = runtime?.RaidSite;
            if (behavior == null)
                return false;

            if (state == null)
            {
                state = new RaidMapCellBehaviorState();
                if (runtime != null)
                    runtime.BehaviorState = state;
            }

            return true;
        }

        private static bool HasFunctionalClick(MapCellRuntime runtime)
        {
            var behavior = runtime?.Definition?.Behavior;
            return behavior != null && behavior.HasFunctionalClick;
        }

        private static bool CanAcceptClick(MapCellRuntime runtime)
        {
            if (runtime.Presence != MapPresence.Interactive)
                return false;

            return HasFunctionalClick(runtime);
        }

        private HashSet<Vector2Int> BuildExpanderSet()
        {
            var expanders = new HashSet<Vector2Int> { Config.HomeCoord };
            var changed = true;

            while (changed)
            {
                changed = false;
                var distances = ComputeDistancesFrom(expanders);
                var interaction = Mathf.Max(0, Config.InteractionRadius);

                foreach (var pair in State.Cells)
                {
                    if (!distances.TryGetValue(pair.Key, out var distance))
                        continue;

                    if (distance > interaction)
                        continue;

                    if (!IsExpander(pair.Value))
                        continue;

                    if (expanders.Add(pair.Key))
                        changed = true;
                }
            }

            return expanders;
        }

        private static bool IsExpander(MapCellRuntime runtime)
        {
            var behavior = runtime?.Definition?.Behavior;
            return behavior != null && behavior.ShouldRevealNeighbors(runtime);
        }

        private Dictionary<Vector2Int, int> ComputeDistancesFrom(HashSet<Vector2Int> sources)
        {
            var distances = new Dictionary<Vector2Int, int>();
            var queue = new Queue<Vector2Int>();

            foreach (var source in sources)
            {
                if (!State.Cells.ContainsKey(source))
                    continue;

                if (!distances.TryAdd(source, 0))
                    continue;

                queue.Enqueue(source);
            }

            var offsets = GetOffsets(Config.NeighborMode);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var currentDist = distances[current];

                for (var i = 0; i < offsets.Length; i++)
                {
                    var next = current + offsets[i];
                    if (!State.Cells.ContainsKey(next))
                        continue;

                    if (distances.ContainsKey(next))
                        continue;

                    distances[next] = currentDist + 1;
                    queue.Enqueue(next);
                }
            }

            return distances;
        }

        private static Vector2Int[] GetOffsets(MapNeighborMode mode)
            => mode == MapNeighborMode.Nine ? NineOffsets : FourOffsets;
    }

    public readonly struct MapCellDisplayInfo
    {
        public Vector2Int Coord { get; }
        public MapCellDefinition Definition { get; }
        public MapPresence Presence { get; }
        public bool HasFunctionalClick { get; }
        public bool CanClick { get; }
        public Sprite Sprite { get; }

        public MapCellDisplayInfo(
            Vector2Int coord,
            MapCellDefinition definition,
            MapPresence presence,
            bool hasFunctionalClick,
            bool canClick,
            Sprite sprite)
        {
            Coord = coord;
            Definition = definition;
            Presence = presence;
            HasFunctionalClick = hasFunctionalClick;
            CanClick = canClick;
            Sprite = sprite;
        }
    }
}
