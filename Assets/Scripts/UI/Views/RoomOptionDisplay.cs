using IdleTower.Data.Definitions;

namespace IdleTower.UI.Views
{
    public readonly struct RoomOptionDisplay
    {
        public RoomDefinition Room { get; }
        public bool CanAfford { get; }
        public string CostText { get; }

        public RoomOptionDisplay(RoomDefinition room, bool canAfford, string costText)
        {
            Room = room;
            CanAfford = canAfford;
            CostText = costText;
        }
    }
}
