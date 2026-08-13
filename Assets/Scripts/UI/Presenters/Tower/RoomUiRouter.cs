using IdleTower.Rooms;
using UnityEngine;

namespace IdleTower.UI.Presenters.Rooms
{
    /// <summary>
    /// RoomUiRouter — маршрут клика по построенной комнате → нужный UI.
    ///
    /// Получает: RoomClickResult и roomIndex от TowerPresenter
    /// Отправляет: вызов OpenProductionMode (и др. по RoomUiId) на TowerPresenter
    ///
    /// View:        —
    /// Systems:      —
    /// Presenters:   TowerPresenter (вызывает методы)
    /// GameEvents:   —
    /// </summary>
    public static class RoomUiRouter
    {
        public static void Open(TowerPresenter towerPresenter, RoomClickResult result, int roomIndex)
        {
            switch (result.OpenUi)
            {
                case RoomUiId.ProductionMode:
                    towerPresenter.OpenProductionMode(roomIndex);
                    break;
                case RoomUiId.Shop:
                    Debug.Log("[RoomUiRouter] Shop UI — после MVP.");
                    break;
                case RoomUiId.None:
                default:
                    break;
            }
        }
    }
}
