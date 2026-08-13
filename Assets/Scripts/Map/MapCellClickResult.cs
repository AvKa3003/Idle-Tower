namespace IdleTower.Map
{
    /// <summary>
    /// Результат клика по клетке.
    /// Behavior может сначала изменить state/кошелёк через context, затем вернуть Action для Presenter.
    /// </summary>
    public readonly struct MapCellClickResult
    {
        public MapCellClickAction Action { get; }

        public MapCellClickResult(MapCellClickAction action)
        {
            Action = action;
        }

        public static MapCellClickResult None => new(MapCellClickAction.None);
    }
}
