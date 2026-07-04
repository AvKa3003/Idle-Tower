using IdleTower.Data.Definitions;

namespace IdleTower.Data.Runtime
{
    public class FloorData
    {
        public RoomDefinition BuiltRoom;
        public RoomBehaviorState State;

        public bool IsBuildSlot => BuiltRoom == null;
    }
}
