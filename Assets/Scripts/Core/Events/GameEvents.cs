using System;
using IdleTower.Data.Definitions;
using IdleTower.Rooms;

namespace IdleTower.Core.Events
{
    public static class GameEvents
    {
        public static event Action<ResourceDefinition, int> ResourceChanged;
        public static event Action<int, RoomDefinition> RoomBuilt;
        public static event Action<TickContext> GameTick;
        public static event Action<int, int> ProductionModeChanged;
        public static event Action<int, int> OperationModeUnlocked;

        // TODO (после MVP): второй экран — раскомментировать и вызывать из ScreenManager.Show
        // public static event Action<ScreenId> ScreenChanged;
        // public static void RaiseScreenChanged(ScreenId id) => ScreenChanged?.Invoke(id);

        public static void RaiseResourceChanged(ResourceDefinition resource, int newAmount)
            => ResourceChanged?.Invoke(resource, newAmount);

        public static void RaiseRoomBuilt(int floorIndex, RoomDefinition room)
            => RoomBuilt?.Invoke(floorIndex, room);

        public static void RaiseGameTick(TickContext context)
            => GameTick?.Invoke(context);

        public static void RaiseProductionModeChanged(int floorIndex, int modeIndex)
            => ProductionModeChanged?.Invoke(floorIndex, modeIndex);

        public static void RaiseOperationModeUnlocked(int floorIndex, int modeIndex)
            => OperationModeUnlocked?.Invoke(floorIndex, modeIndex);
    }
}
