using System.Collections.Generic;
using IdleTower.Core;
using IdleTower.Data.Definitions;
using IdleTower.Data.Save;
using IdleTower.Map.Behaviors;

namespace IdleTower.Map
{
    /// <summary>Экстренное завершение по saved behaviorType (когда SO behavior уже другой).</summary>
    public static class MapCellBehaviorEmergency
    {
        public static void FinishFromSave(
            string savedBehaviorType,
            MapCellSave save,
            GameServices services,
            IReadOnlyDictionary<ResourceId, ResourceDefinition> catalog)
        {
            if (save == null || services == null)
                return;

            switch (savedBehaviorType)
            {
                case MapCellBehaviorIds.Raid:
                    RaidMapCellBehavior.EmergencyFinishFromSave(save, services, catalog);
                    break;
            }
        }
    }
}
