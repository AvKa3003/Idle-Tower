namespace IdleTower.Rooms
{
    public readonly struct RoomClickResult
    {
        public RoomUiId OpenUi { get; }

        public RoomClickResult(RoomUiId openUi)
        {
            OpenUi = openUi;
        }

        public static RoomClickResult None => new(RoomUiId.None);
    }
}
