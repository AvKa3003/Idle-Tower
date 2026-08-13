using IdleTower.Data.Definitions;
using IdleTower.Map;

namespace IdleTower.Data.Runtime
{
    public sealed class MapCellRuntime
    {
        public MapCellDefinition Definition { get; }
        public MapPresence Presence { get; set; }
        public MapCellRuntimeState BehaviorState { get; set; }

        public MapCellRuntime(MapCellDefinition definition, MapCellRuntimeState behaviorState)
        {
            Definition = definition;
            BehaviorState = behaviorState ?? MapCellRuntimeState.Empty;
            Presence = MapPresence.Fog;
        }
    }
}
