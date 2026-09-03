using IdleTower.Data.Definitions;
using IdleTower.Map;

namespace IdleTower.Data.Runtime
{
    public sealed class MapCellRuntime
    {
        public MapCellDefinition Definition { get; }
        public MapPresence Presence { get; set; }
        public MapCellRuntimeState BehaviorState { get; set; }

        /// <summary>Per-entry конфиг с MapConfig (Raid и др.).</summary>
        public MapCellSiteConfig Site { get; }

        public RaidSiteConfig RaidSite => Site?.Raid;

        public MapCellRuntime(
            MapCellDefinition definition,
            MapCellRuntimeState behaviorState,
            MapCellSiteConfig site = null)
        {
            Definition = definition;
            BehaviorState = behaviorState ?? MapCellRuntimeState.Empty;
            Site = site;
            Presence = MapPresence.Fog;
        }
    }
}
