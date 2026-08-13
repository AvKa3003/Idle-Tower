namespace IdleTower.Map
{
    /// <summary>Правило соседей на сетке (для interaction / vision и BFS).</summary>
    public enum MapNeighborMode
    {
        /// <summary>4 стороны (без диагоналей).</summary>
        Four = 0,

        /// <summary>8 направлений (клетка 3×3 без центра; в плане «9»).</summary>
        Nine = 1
    }
}
