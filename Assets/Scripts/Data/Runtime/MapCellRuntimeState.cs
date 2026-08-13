namespace IdleTower.Data.Runtime
{
    /// <summary>Runtime-состояние behavior клетки. На этапе A пустое; позже JSON рейда/лута.</summary>
    public sealed class MapCellRuntimeState
    {
        public static MapCellRuntimeState Empty { get; } = new();
    }
}
