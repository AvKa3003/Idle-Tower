using System.Collections.Generic;
using IdleTower.Core;
using IdleTower.Data.Runtime;
using IdleTower.Rooms;
using IdleTower.Rooms.Behaviors;
using IdleTower.Rooms.Production;

namespace IdleTower.Systems
{
    /// <summary>
    /// Команды и чтение режимов производства. Не смешивается с общим RoomBehaviorSystem.
    /// </summary>
    public class ProductionSystem
    {
        private readonly GameServices _services;

        public ProductionSystem(GameServices services)
        {
            _services = services;
        }

        public bool TryUnlockMode(int roomIndex, ModeId modeId)
        {
            if (!TryGetProduction(roomIndex, out var production, out var context))
                return false;

            return production.TryUnlockMode(context, modeId);
        }

        public bool TrySetMode(int roomIndex, ModeId modeId)
        {
            if (!TryGetProduction(roomIndex, out var production, out var context))
                return false;

            return production.TrySetMode(context, modeId);
        }

        public bool TryTogglePause(int roomIndex)
        {
            if (!TryGetProduction(roomIndex, out var production, out var context))
                return false;

            return production.TryTogglePause(context);
        }

        public bool IsPaused(int roomIndex)
        {
            if (!TryGetProduction(roomIndex, out var production, out var context))
                return false;

            return production.IsPaused(context);
        }

        public IReadOnlyList<OperationModeInfo> GetOperationModes(int roomIndex)
        {
            if (!TryGetProduction(roomIndex, out var production, out var context))
                return System.Array.Empty<OperationModeInfo>();

            return production.GetOperationModes(context);
        }

        private bool TryGetProduction(
            int roomIndex,
            out ProductionRoomBehavior production,
            out RoomBehaviorContext context)
        {
            production = null;
            context = default;

            var towerRoom = _services.Tower.GetRoom(roomIndex);
            if (towerRoom == null || towerRoom.IsEmpty)
                return false;

            if (towerRoom.BuiltRoom?.Behavior is not ProductionRoomBehavior productionBehavior)
                return false;

            production = productionBehavior;
            context = new RoomBehaviorContext(roomIndex, towerRoom, _services.Tower, _services);
            return true;
        }
    }
}
