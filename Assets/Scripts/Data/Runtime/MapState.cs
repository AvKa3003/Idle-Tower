using System.Collections.Generic;
using IdleTower.Data.Definitions;
using UnityEngine;

namespace IdleTower.Data.Runtime
{
    public sealed class MapState
    {
        private readonly Dictionary<Vector2Int, MapCellRuntime> _cells = new();

        public Vector2Int HomeCoord { get; private set; }

        public IReadOnlyDictionary<Vector2Int, MapCellRuntime> Cells => _cells;

        public void LoadFromConfig(MapConfig config)
        {
            _cells.Clear();
            HomeCoord = config != null ? config.HomeCoord : Vector2Int.zero;

            if (config?.Entries == null)
                return;

            for (var i = 0; i < config.Entries.Length; i++)
            {
                var entry = config.Entries[i];
                if (entry.Cell == null)
                    continue;

                if (_cells.ContainsKey(entry.Coord))
                    continue;

                var behavior = entry.Cell.Behavior;
                var state = behavior != null
                    ? behavior.CreateDefaultState()
                    : MapCellRuntimeState.Empty;

                _cells[entry.Coord] = new MapCellRuntime(entry.Cell, state, entry.Site);
            }
        }

        public bool TryGet(Vector2Int coord, out MapCellRuntime runtime)
            => _cells.TryGetValue(coord, out runtime);

        public void Clear()
        {
            _cells.Clear();
            HomeCoord = Vector2Int.zero;
        }
    }
}
