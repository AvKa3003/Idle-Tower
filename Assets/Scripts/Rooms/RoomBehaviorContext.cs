using IdleTower.Core;
using IdleTower.Data.Runtime;

namespace IdleTower.Rooms
{
    public readonly struct RoomBehaviorContext
    {
        public int RoomIndex { get; }
        public TowerRoom TowerRoom { get; }
        public TowerState Tower { get; }
        public GameServices Services { get; }

        public RoomBehaviorContext(int roomIndex, TowerRoom towerRoom, TowerState tower, GameServices services)
        {
            RoomIndex = roomIndex;
            TowerRoom = towerRoom;
            Tower = tower;
            Services = services;
        }
    }
}
