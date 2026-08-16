using System;
using System.Collections.Generic;
using IdleTower.Core;
using IdleTower.Core.Events;
using IdleTower.Data.Definitions;
using IdleTower.Data.Runtime;
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
            if (!TryGetRaid(coord, out var behavior, out var state))
                return false;

            if (!AllowsPlayPause(behavior, state))
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

            if (!TryGetRaid(coord, out var behavior, out var state))
                return false;

            if (!AllowsPlayPause(behavior, state))
                return false;

            var next = Math.Max(0, amount);
            state.PlannedArmy = RaidArmyHelper.WithUnitAmount(state.PlannedArmy, unit, next);
            state.IsPaused = true;
            return true;
        }

        public bool TryStartRaidIfPossible(Vector2Int coord)
        {
            if (!TryGetRaid(coord, out var behavior, out var state))
                return false;

            if (!CanStartRaid(coord, behavior, state, out var config, out var army))
                return false;

            if (!RaidArmyHelper.TrySpendArmy(_services.Resources, army))
                return false;

            state.HasActiveRaid = true;
            state.ActiveElapsedSeconds = 0f;
            return true;
        }

        public bool TryGetRaidInfo(Vector2Int coord, out RaidCellInfo info)
        {
            info = default;
            if (!TryGetRaid(coord, out var behavior, out var state))
                return false;

            var config = behavior.GetActiveRaidConfig(state) ?? behavior.PreCapture;
            var duration = config != null ? Mathf.Max(0f, config.Duration.TotalSeconds) : 0f;
            var elapsed = state.HasActiveRaid ? state.ActiveElapsedSeconds : 0f;
            var progress = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 0f;

            var planned = RaidArmyHelper.CloneNonEmpty(state.PlannedArmy);
            var plannedStrength = RaidArmyHelper.CalcStrength(planned);
            var meets = config != null && RaidArmyHelper.MeetsConfigRequirements(config, planned);
            var canStart = CanStartRaid(coord, behavior, state, out _, out _);

            info = new RaidCellInfo(
                coord,
                runtimeTitle(coord),
                state.Phase,
                state.IsPaused,
                state.HasActiveRaid,
                state.CompletedRaids,
                behavior.MaxCompletedRaids,
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
                behavior.PostCaptureMode);
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
                if (!TryGetRaidRuntime(pair.Value, out var behavior, out var state))
                    continue;

                if (!state.HasActiveRaid)
                    continue;

                // В случае ошибки в данных - исправляем ошибку
                var config = behavior.GetActiveRaidConfig(state);
                if (config == null)
                {
                    state.HasActiveRaid = false;
                    state.ActiveElapsedSeconds = 0f;
                    continue;
                }

                var duration = Mathf.Max(0.0001f, config.Duration.TotalSeconds);
                state.ActiveElapsedSeconds += tickDelta;

                if (state.ActiveElapsedSeconds < duration)
                    continue;

                CompleteRaid(pair.Key, behavior, state, config, ref presenceDirty);
            }

            return presenceDirty;
        }

        private void CompleteRaid(
            Vector2Int coord,
            RaidMapCellBehavior behavior,
            RaidMapCellBehaviorState state,
            RaidConfig config,
            ref bool presenceDirty)
        {
            state.HasActiveRaid = false;
            state.ActiveElapsedSeconds = 0f;

            if (config.Rewards != null && config.Rewards.Length > 0)
                _services.Resources.Add(config.Rewards);

            if (state.IsCaptured)
                return;

            state.CompletedRaids++;
            if (state.CompletedRaids < behavior.MaxCompletedRaids)
                return;

            state.Phase = RaidCellPhase.Captured;
            // Farm иначе сразу автостартует и может съесть армию/ресурсы без согласия игрока.
            state.IsPaused = behavior.PostCaptureMode == PostCaptureMode.RaidFarm;
            presenceDirty = true;
        }

        private void TryAutostartRaids()
        {
            _autostartBuffer.Clear();

            foreach (var pair in State.Cells)
            {
                if (pair.Value.Presence != MapPresence.Interactive)
                    continue;

                if (!TryGetRaidRuntime(pair.Value, out var behavior, out var state))
                    continue;

                if (!CanStartRaid(pair.Key, behavior, state, out _, out _))
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
            RaidMapCellBehavior behavior,
            RaidMapCellBehaviorState state,
            out RaidConfig config,
            out ResourceCost[] army)
        {
            config = null;
            army = Array.Empty<ResourceCost>();

            if (!State.TryGet(coord, out var runtime))
                return false;

            if (runtime.Presence != MapPresence.Interactive)
                return false;

            if (state.IsPaused || state.HasActiveRaid)
                return false;

            config = behavior.GetActiveRaidConfig(state);
            if (config == null)
                return false;

            if (!state.IsCaptured && state.CompletedRaids >= behavior.MaxCompletedRaids)
                return false;

            army = RaidArmyHelper.CloneNonEmpty(state.PlannedArmy);
            if (!RaidArmyHelper.MeetsConfigRequirements(config, army))
                return false;

            return RaidArmyHelper.CanAffordArmy(_services.Wallet, army);
        }

        private static bool AllowsPlayPause(RaidMapCellBehavior behavior, RaidMapCellBehaviorState state)
        {
            if (behavior == null || state == null)
                return false;

            if (!state.IsCaptured)
                return true;

            return behavior.PostCaptureMode == PostCaptureMode.RaidFarm;
        }

        private bool TryGetRaid(
            Vector2Int coord,
            out RaidMapCellBehavior behavior,
            out RaidMapCellBehaviorState state)
        {
            behavior = null;
            state = null;

            if (!State.TryGet(coord, out var runtime))
                return false;

            return TryGetRaidRuntime(runtime, out behavior, out state);
        }

        private static bool TryGetRaidRuntime(
            MapCellRuntime runtime,
            out RaidMapCellBehavior behavior,
            out RaidMapCellBehaviorState state)
        {
            behavior = runtime?.Definition?.Behavior as RaidMapCellBehavior;
            state = runtime?.BehaviorState as RaidMapCellBehaviorState;
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
