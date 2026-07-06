using IdleTower.Core;
using IdleTower.Core.Events;
using IdleTower.Data.Definitions;
using IdleTower.Data.Runtime;

namespace IdleTower.Systems
{
    public class BuildingSystem
    {
        private readonly GameServices _services;

        public BuildingSystem(GameServices services)
        {
            _services = services;
        }

        public bool CanAfford(RoomDefinition room)
        {
            if (room == null)
                return false;

            return _services.Resources.CanAfford(room.Cost);
        }

        public BuildResult TryBuild(int floorIndex, RoomDefinition room)
        {
            var tower = _services.Tower;
            if (room == null)
                return BuildResult.InvalidRoom;

            if (room.Behavior == null)
                return BuildResult.MissingBehavior;

            if (floorIndex != tower.BuildSlotFloorIndex)
                return BuildResult.InvalidFloor;

            if (!_services.UnlockTree.IsAvailable(room, tower))
                return BuildResult.NotAvailable;

            if (!CanAfford(room))
                return BuildResult.CannotAfford;

            if (!_services.Resources.TrySpend(room.Cost))
                return BuildResult.CannotAfford;

            var initialState = room.Behavior.CreateDefaultState();
            tower.AddBuiltFloor(room, initialState);
            _services.RoomBehaviors.OnRoomBuilt(floorIndex);

            GameEvents.RaiseRoomBuilt(floorIndex, room);
            return BuildResult.Success;
        }
    }
}
