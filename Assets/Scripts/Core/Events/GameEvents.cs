using System;
using IdleTower.Data.Definitions;
using IdleTower.Rooms;
using IdleTower.Rooms.Production;

namespace IdleTower.Core.Events
{
    public static class GameEvents
    {
        public static event Action<ResourceDefinition, int> ResourceChanged;
        public static event Action<int, RoomDefinition> RoomBuilt;
        public static event Action<TickContext> GameTick;
        public static event Action<int, ModeId> ProductionModeChanged;
        public static event Action<int, ModeId> OperationModeUnlocked;

        // TODO: второй экран — раскомментировать и вызывать из ScreenManager.Show
        // public static event Action<ScreenId> ScreenChanged;
        // public static void RaiseScreenChanged(ScreenId id) => ScreenChanged?.Invoke(id);

        public static void RaiseResourceChanged(ResourceDefinition resource, int newAmount)
            => ResourceChanged?.Invoke(resource, newAmount);

        public static void RaiseRoomBuilt(int roomIndex, RoomDefinition room)
            => RoomBuilt?.Invoke(roomIndex, room);

        public static void RaiseGameTick(TickContext context)
            => GameTick?.Invoke(context);

        public static void RaiseProductionModeChanged(int roomIndex, ModeId modeId)
            => ProductionModeChanged?.Invoke(roomIndex, modeId);

        public static void RaiseOperationModeUnlocked(int roomIndex, ModeId modeId)
            => OperationModeUnlocked?.Invoke(roomIndex, modeId);
    }
}
