using UnityEngine;

namespace IdleTower.Map.Behaviors
{
    /// <summary>Заглушка соседа до Raid/OneShot (этап A): клик → простой UI.</summary>
    [CreateAssetMenu(fileName = "StubMapCell", menuName = "IdleTower/Map Cell Behavior/Stub")]
    public class StubMapCellBehavior : MapCellBehaviorBase
    {
        public override bool RevealsNeighborsWhenInteractive => false;

        public override MapCellClickResult OnClicked(MapCellBehaviorContext context)
            => new(MapCellClickAction.OpenStub);
    }
}
