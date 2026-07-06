namespace IdleTower.Core
{
    /// <summary>Участник фиксированного тик-цикла GameTickSystem.</summary>
    public interface ITickable
    {
        void OnTick(TickContext context);
    }
}
