using System;
using IdleTower.Data.Definitions;
using IdleTower.Rooms.Production;

namespace IdleTower.Core.Events
{
    public static class GameEvents
    {
        public static bool Suppress { get; set; }

        public static event Action<ResourceDefinition, int> ResourceChanged;
        public static event Action<int, RoomDefinition> RoomBuilt;
        public static event Action<TickContext> GameTick;
        public static event Action<int, ModeId> ProductionModeChanged;
        public static event Action<int, ModeId> OperationModeUnlocked;
        public static event Action<ScreenId> ScreenChanged;
        public static event Action MapPresenceChanged;

        public static void RaiseResourceChanged(ResourceDefinition resource, int newAmount)
        {
            if (Suppress) return;
            ResourceChanged?.Invoke(resource, newAmount);
        }

        public static void RaiseRoomBuilt(int roomIndex, RoomDefinition room)
        {
            if (Suppress) return;
            RoomBuilt?.Invoke(roomIndex, room);
        }

        public static void RaiseGameTick(TickContext context)
        {
            if (Suppress) return;
            GameTick?.Invoke(context);
        }

        public static void RaiseProductionModeChanged(int roomIndex, ModeId modeId)
        {
            if (Suppress) return;
            ProductionModeChanged?.Invoke(roomIndex, modeId);
        }

        public static void RaiseOperationModeUnlocked(int roomIndex, ModeId modeId)
        {
            if (Suppress) return;
            OperationModeUnlocked?.Invoke(roomIndex, modeId);
        }

        public static void RaiseScreenChanged(ScreenId id)
        {
            if (Suppress) return;
            ScreenChanged?.Invoke(id);
        }

        public static void RaiseMapPresenceChanged()
        {
            if (Suppress) return;
            MapPresenceChanged?.Invoke();
        }
    }
}
