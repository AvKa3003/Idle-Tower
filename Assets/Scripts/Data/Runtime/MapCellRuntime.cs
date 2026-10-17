using IdleTower.Data.Definitions;
using IdleTower.Map;

namespace IdleTower.Data.Runtime
{
    public sealed class MapCellRuntime
    {
        /// <summary>Стабильный id экземпляра с MapConfigEntry (сейв / move).</summary>
        public string InstanceId { get; }

        public MapCellDefinition Definition { get; }
        public MapPresence Presence { get; set; }
        public MapCellRuntimeState BehaviorState { get; set; }

        /// <summary>Per-entry конфиг с MapConfig (Raid и др.).</summary>
        public MapCellSiteConfig Site { get; }

        public RaidSiteConfig RaidSite => Site?.Raid;

        public MapCellRuntime(
            string instanceId,
            MapCellDefinition definition,
            MapCellRuntimeState behaviorState,
            MapCellSiteConfig site = null)
        {
            InstanceId = instanceId ?? string.Empty;
            Definition = definition;
            BehaviorState = behaviorState ?? MapCellRuntimeState.Empty;
            Site = site;
            Presence = MapPresence.Fog;
        }
    }
}
