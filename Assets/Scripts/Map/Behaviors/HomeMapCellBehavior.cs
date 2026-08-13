using UnityEngine;

namespace IdleTower.Map.Behaviors
{
    [CreateAssetMenu(fileName = "HomeMapCell", menuName = "IdleTower/Map Cell Behavior/Home")]
    public class HomeMapCellBehavior : MapCellBehaviorBase
    {
        public override bool RevealsNeighborsWhenInteractive => true;

        public override MapCellClickResult OnClicked(MapCellBehaviorContext context)
            => new(MapCellClickAction.GoToMainTower);
    }
}
