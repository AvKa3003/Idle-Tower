using System.Collections.Generic;
using IdleTower.Core;
using IdleTower.Core.Events;
using IdleTower.Data.Runtime;
using IdleTower.Rooms;
using IdleTower.Rooms.Behaviors;

namespace IdleTower.Systems
{
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
            for (var i = 0; i < tower.Floors.Count; i++)
            {
                var floor = tower.Floors[i];
                if (floor.IsBuildSlot || floor.BuiltRoom?.Behavior == null)
                    continue;

                var context = CreateContext(i);
                floor.BuiltRoom.Behavior.Tick(context, tickContext);
            }
        }

        public void OnRoomBuilt(int floorIndex)
        {
            if (!TryCreateContext(floorIndex, out var context))
                return;

            context.Floor.BuiltRoom?.Behavior?.OnRoomBuilt(context);
        }

        public RoomClickResult OnRoomClicked(int floorIndex)
        {
            if (!TryCreateContext(floorIndex, out var context) || context.Floor.BuiltRoom?.Behavior == null)
                return RoomClickResult.None;

            return context.Floor.BuiltRoom.Behavior.OnRoomClicked(context);
        }

        public bool TryUnlockMode(int floorIndex, int modeIndex)
        {
            if (!TryCreateContext(floorIndex, out var context))
                return false;

            if (context.Floor.BuiltRoom?.Behavior is not ProductionRoomBehavior production)
                return false;

            return production.TryUnlockMode(context, modeIndex);
        }

        public bool TrySetMode(int floorIndex, int modeIndex)
        {
            if (!TryCreateContext(floorIndex, out var context))
                return false;

            if (context.Floor.BuiltRoom?.Behavior is not ProductionRoomBehavior production)
                return false;

            return production.TrySetMode(context, modeIndex);
        }

        public IReadOnlyList<OperationModeInfo> GetOperationModes(int floorIndex)
        {
            if (!TryCreateContext(floorIndex, out var context))
                return System.Array.Empty<OperationModeInfo>();

            if (context.Floor.BuiltRoom?.Behavior is not ProductionRoomBehavior production)
                return System.Array.Empty<OperationModeInfo>();

            return production.GetOperationModes(context);
        }

        public RoomStatusInfo GetRoomStatusInfo(int floorIndex)
        {
            if (!TryCreateContext(floorIndex, out var context) || context.Floor.BuiltRoom?.Behavior == null)
                return null;

            return context.Floor.BuiltRoom.Behavior.GetRoomStatusInfo(context);
        }

        private bool TryCreateContext(int floorIndex, out RoomBehaviorContext context)
        {
            var floor = _services.Tower.GetFloor(floorIndex);
            if (floor == null || floor.IsBuildSlot)
            {
                context = default;
                return false;
            }

            context = new RoomBehaviorContext(floorIndex, floor, _services.Tower, _services);
            return true;
        }

        private RoomBehaviorContext CreateContext(int floorIndex)
        {
            TryCreateContext(floorIndex, out var context);
            return context;
        }
    }
}
