using IdleTower.Data.Definitions;

namespace IdleTower.UI.Views
{
    public readonly struct RoomOptionDisplay
    {
        public RoomDefinition Room { get; }
        public bool CanAfford { get; }

        public RoomOptionDisplay(RoomDefinition room, bool canAfford)
        {
            Room = room;
            CanAfford = canAfford;
        }
    }
}
