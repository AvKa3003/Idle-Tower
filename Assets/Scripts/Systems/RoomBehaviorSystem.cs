using IdleTower.Core;
using IdleTower.Core.Events;
using IdleTower.Data.Runtime;
using IdleTower.Rooms;

namespace IdleTower.Systems
{
    /// <summary>
    /// Диспетчер поведений комнат: Tick, OnBuilt, OnClick, Status.
    /// Production-режимы — в <see cref="ProductionSystem"/>.
    /// </summary>
    public class RoomBehaviorSystem : ITickable
    {
        private readonly GameServices _services;

        public RoomBehaviorSystem(GameServices services)
        {
            _services = services;
        }

        public void OnTick(TickContext tickContext)
        {
            var tower = _services.Tower;
            for (var i = 0; i < tower.Rooms.Count; i++)
            {
                var room = tower.Rooms[i];
                if (room.IsEmpty || room.BuiltRoom?.Behavior == null)
                    continue;

                var context = CreateContext(i);
                room.BuiltRoom.Behavior.Tick(context, tickContext);
            }
        }

        public void OnRoomBuilt(int roomIndex)
        {
            if (!TryCreateContext(roomIndex, out var context))
                return;

            context.TowerRoom.BuiltRoom?.Behavior?.OnRoomBuilt(context);
        }

        public RoomClickResult OnRoomClicked(int roomIndex)
        {
            if (!TryCreateContext(roomIndex, out var context) || context.TowerRoom.BuiltRoom?.Behavior == null)
                return RoomClickResult.None;

            return context.TowerRoom.BuiltRoom.Behavior.OnRoomClicked(context);
        }

        public RoomStatusInfo GetRoomStatusInfo(int roomIndex)
        {
            if (!TryCreateContext(roomIndex, out var context) || context.TowerRoom.BuiltRoom?.Behavior == null)
                return null;

            return context.TowerRoom.BuiltRoom.Behavior.GetRoomStatusInfo(context);
        }

        private bool TryCreateContext(int roomIndex, out RoomBehaviorContext context)
        {
            var towerRoom = _services.Tower.GetRoom(roomIndex);
            if (towerRoom == null || towerRoom.IsEmpty)
            {
                context = default;
                return false;
            }

            context = new RoomBehaviorContext(roomIndex, towerRoom, _services.Tower, _services);
            return true;
        }

        private RoomBehaviorContext CreateContext(int roomIndex)
        {
            TryCreateContext(roomIndex, out var context);
            return context;
        }
    }
}
