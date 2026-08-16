namespace IdleTower.Map
{
    /// <summary>
    /// Исход клика для Presenter (навигация / панель).
    /// Изменение игровых данных — в behavior/MapSystem через context до возврата результата.
    /// </summary>
    public enum MapCellClickAction
    {
        /// <summary>Ничего не показывать (тихий эффект уже мог произойти в behavior).</summary>
        None = 0,

        GoToMainTower = 1,

        /// <summary>Панель набега.</summary>
        OpenRaid = 2
    }
}
