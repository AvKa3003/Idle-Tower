namespace IdleTower.Data.Runtime
{
    /// <summary>Runtime-состояние behavior клетки. Подклассы — у конкретных behaviors.</summary>
    public class MapCellRuntimeState
    {
        public static MapCellRuntimeState Empty { get; } = new();
    }
}
