using System.Collections.Generic;
using System.Linq;
using IdleTower.Data.Definitions;

namespace IdleTower.Data.Runtime
{
    public class TowerState
    {
        private readonly List<FloorData> _floors = new();

        public IReadOnlyList<FloorData> Floors => _floors;

        public void ResetWithEmptyBuildSlot()
        {
            _floors.Clear();
            _floors.Add(new FloorData());
        }

        public int BuildSlotFloorIndex
        {
            get
            {
                if (_floors.Count == 0)
                    return -1;

                return _floors.Count - 1;
            }
        }

        public FloorData GetFloor(int floorIndex)
        {
            if (floorIndex < 0 || floorIndex >= _floors.Count)
                return null;

            return _floors[floorIndex];
        }

        public bool IsRoomBuilt(RoomDefinition room)
        {
            if (room == null)
                return false;

            return _floors.Any(f => f.BuiltRoom == room);
        }

        public void AddBuiltFloor(RoomDefinition room, RoomBehaviorState initialState)
        {
            var buildIndex = BuildSlotFloorIndex;
            if (buildIndex < 0)
                return;

            _floors[buildIndex].BuiltRoom = room;
            _floors[buildIndex].State = initialState;
            _floors.Add(new FloorData());
        }

        public int BuiltRoomCount
        {
            get
            {
                var count = 0;
                foreach (var floor in _floors)
                {
                    if (!floor.IsBuildSlot)
                        count++;
                }

                return count;
            }
        }
    }
}
