namespace IdleTower.Core
{
    public readonly struct TickContext
    {
        public ulong CurrentTick { get; }
        public float TickDelta { get; }
        public int TicksPerSecond { get; }
        public GameServices Services { get; }

        public TickContext(ulong currentTick, float tickDelta, int ticksPerSecond, GameServices services)
        {
            CurrentTick = currentTick;
            TickDelta = tickDelta;
            TicksPerSecond = ticksPerSecond;
            Services = services;
        }
    }
}
