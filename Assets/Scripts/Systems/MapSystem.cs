using System.Collections.Generic;
using IdleTower.Core;
using IdleTower.Data.Definitions;
using IdleTower.Data.Runtime;
using IdleTower.Map;
using UnityEngine;

namespace IdleTower.Systems
{
    /// <summary>
    /// Карта: загрузка MapConfig, пересчёт Fog/VisibleOnly/Interactive, клики клеток.
    /// Тики рейдов — на следующих этапах.
    /// </summary>
    public sealed class MapSystem
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

            var context = new MapCellBehaviorContext(coord, runtime.Definition, runtime, _services);
            return behavior.OnClicked(context);
        }

        public bool TryGetCellDisplay(Vector2Int coord, out MapCellDisplayInfo info)
        {
            info = default;
            if (!State.TryGet(coord, out var runtime) || runtime.Definition == null)
                return false;

            info = new MapCellDisplayInfo(
                coord,
                runtime.Definition,
                runtime.Presence,
                HasFunctionalClick(runtime),
                CanAcceptClick(runtime));
            return true;
        }

        public IEnumerable<MapCellDisplayInfo> EnumerateDisplays()
        {
            foreach (var pair in State.Cells)
            {
                var runtime = pair.Value;
                if (runtime?.Definition == null)
                    continue;

                yield return new MapCellDisplayInfo(
                    pair.Key,
                    runtime.Definition,
                    runtime.Presence,
                    HasFunctionalClick(runtime),
                    CanAcceptClick(runtime));
            }
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

        /// <summary>
        /// Рекурсивно собираем клетки которые являются расширителями видимой области.
        /// <para>
        /// Расширителями являются все клетки с Presence = Interactive и RevealsNeighborsWhenInteractive = true
        /// </para>
        /// </summary>
        private HashSet<Vector2Int> BuildExpanderSet()
        {
            var expanders = new HashSet<Vector2Int> { Config.HomeCoord };
            var changed = true;

            while (changed)
            {
                changed = false;
                // Собираем дистанции всех клеток от известеных расширителей чтобы узнать какие соседние клетки тоже являются расширителями
                // до тех пор пока список расширителей не перестанет дополняться
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
            return behavior != null && behavior.RevealsNeighborsWhenInteractive;
        }

        // Вычисляем список кротчайших расстояний до источников для всех клетов через поиск в ширину.
        // В очередь добавляются истоки.
        // Пока очередь не станет пустой добавляем в конец все соседние клетки для которых не была расчитана дистацния от каждой клетки из начала очереди.
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

        public MapCellDisplayInfo(
            Vector2Int coord,
            MapCellDefinition definition,
            MapPresence presence,
            bool hasFunctionalClick,
            bool canClick)
        {
            Coord = coord;
            Definition = definition;
            Presence = presence;
            HasFunctionalClick = hasFunctionalClick;
            CanClick = canClick;
        }
    }
}
