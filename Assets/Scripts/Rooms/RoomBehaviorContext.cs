using IdleTower.Core;
using IdleTower.Data.Runtime;

namespace IdleTower.Rooms
{
    public readonly struct RoomBehaviorContext
    {
        public int FloorIndex { get; }
        public FloorData Floor { get; }
        public TowerState Tower { get; }
        public GameServices Services { get; }

        public RoomBehaviorContext(int floorIndex, FloorData floor, TowerState tower, GameServices services)
        {
            FloorIndex = floorIndex;
            Floor = floor;
            Tower = tower;
            Services = services;
        }
    }
}
