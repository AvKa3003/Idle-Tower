using UnityEngine;

namespace IdleTower.Map.Behaviors
{
    [CreateAssetMenu(fileName = "DecorativeMapCell", menuName = "IdleTower/Map Cell Behavior/Decorative")]
    public class DecorativeMapCellBehavior : MapCellBehaviorBase
    {
        [SerializeField] private bool revealsNeighbors;

        public override bool RevealsNeighborsWhenInteractive => revealsNeighbors;

        public override string GetBehaviorTypeId() => MapCellBehaviorIds.Decorative;

        public override bool HasFunctionalClick => false;

        public override MapCellClickResult OnClicked(MapCellBehaviorContext context)
            => MapCellClickResult.None;
    }
}
