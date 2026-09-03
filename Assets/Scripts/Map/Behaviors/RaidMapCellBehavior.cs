using IdleTower.Data.Definitions;
using IdleTower.Data.Runtime;
using UnityEngine;

namespace IdleTower.Map.Behaviors
{
    /// <summary>
    /// Тип клетки «рейд». Баланс — в MapConfig.Entry.Site.Raid, не в этом SO.
    /// </summary>
    [CreateAssetMenu(fileName = "RaidMapCell", menuName = "IdleTower/Map Cell Behavior/Raid")]
    public class RaidMapCellBehavior : MapCellBehaviorBase
    {
        /// <summary>До захвата не expander; после — через ShouldRevealNeighbors(runtime).</summary>
        public override bool RevealsNeighborsWhenInteractive => false;

        public override bool ShouldRevealNeighbors(MapCellRuntime runtime)
            => runtime?.BehaviorState is RaidMapCellBehaviorState state && state.IsCaptured;

        public override MapCellClickResult OnClicked(MapCellBehaviorContext context)
            => new(MapCellClickAction.OpenRaid);

        public override MapCellRuntimeState CreateDefaultState()
            => new RaidMapCellBehaviorState();

        public override Sprite GetDisplaySprite(MapCellRuntime runtime)
        {
            if (runtime?.BehaviorState is RaidMapCellBehaviorState state
                && state.IsCaptured
                && runtime.RaidSite?.CapturedSprite != null)
            {
                return runtime.RaidSite.CapturedSprite;
            }

            return null;
        }
    }
}
