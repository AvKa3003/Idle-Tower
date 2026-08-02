using System.Collections.Generic;
using System.Linq;
using IdleTower.Data.Definitions;

namespace IdleTower.Data.Runtime
{
    public class TowerState
    {
        private readonly List<TowerRoom> _rooms = new();

        public IReadOnlyList<TowerRoom> Rooms => _rooms;

        public void ResetWithEmptyRoom()
        {
            _rooms.Clear();
            _rooms.Add(new TowerRoom());
        }

        public int EmptyRoomIndex
        {
            get
            {
                if (_rooms.Count == 0)
                    return -1;

                return _rooms.Count - 1;
            }
        }

        public TowerRoom GetRoom(int roomIndex)
        {
            if (roomIndex < 0 || roomIndex >= _rooms.Count)
                return null;

            return _rooms[roomIndex];
        }

        public bool IsRoomBuilt(RoomDefinition room)
        {
            if (room == null)
                return false;

            return _rooms.Any(r => r.BuiltRoom == room);
        }

        public void AddBuiltRoom(RoomDefinition room, RoomBehaviorState initialState)
        {
            var buildIndex = EmptyRoomIndex;
            if (buildIndex < 0)
                return;

            _rooms[buildIndex].BuiltRoom = room;
            _rooms[buildIndex].State = initialState;
            _rooms.Add(new TowerRoom());
        }

        /// <summary>
        /// Восстановление башни из сейва: только построенные этажи снизу вверх + пустой слот сверху.
        /// </summary>
        public void ReplaceWithBuiltRooms(IReadOnlyList<(RoomDefinition room, RoomBehaviorState state)> builtRooms)
        {
            _rooms.Clear();

            if (builtRooms != null)
            {
                for (var i = 0; i < builtRooms.Count; i++)
                {
                    var entry = builtRooms[i];
                    _rooms.Add(new TowerRoom
                    {
                        BuiltRoom = entry.room,
                        State = entry.state
                    });
                }
            }

            _rooms.Add(new TowerRoom());
        }

        public int BuiltRoomCount
        {
            get
            {
                var count = 0;
                foreach (var room in _rooms)
                {
                    if (!room.IsEmpty)
                        count++;
                }

                return count;
            }
        }
    }
}
