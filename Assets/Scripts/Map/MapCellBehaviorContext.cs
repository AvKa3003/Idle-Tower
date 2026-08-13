using IdleTower.Core;
using IdleTower.Data.Definitions;
using IdleTower.Data.Runtime;
using UnityEngine;

namespace IdleTower.Map
{
    public readonly struct MapCellBehaviorContext
    {
        public Vector2Int Coord { get; }
        public MapCellDefinition Definition { get; }
        public MapCellRuntime Runtime { get; }
        public GameServices Services { get; }

        public MapCellBehaviorContext(
            Vector2Int coord,
            MapCellDefinition definition,
            MapCellRuntime runtime,
            GameServices services)
        {
            Coord = coord;
            Definition = definition;
            Runtime = runtime;
            Services = services;
        }
    }
}
