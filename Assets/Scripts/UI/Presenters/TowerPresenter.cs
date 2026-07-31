using IdleTower.Core;
using IdleTower.Core.Events;
using IdleTower.Data.Definitions;
using IdleTower.Rooms;
using IdleTower.Rooms.Production;
using IdleTower.Tower;
using IdleTower.UI.Presenters.Rooms;
using UnityEngine;

namespace IdleTower.UI.Presenters
{
    /// <summary>
    /// TowerPresenter — башня на сцене, overlay производства, координатор кликов по комнатам.
    ///
    /// Получает: клики TowerView; GameEvents (RoomBuilt, GameTick, ProductionModeChanged,
    ///           OperationModeUnlocked); данные Tower и RoomBehaviors (чтение)
    /// Отправляет: TowerView.SyncFromState, SetRoomStatus; RoomSelection.Open;
    ///             ProductionMode.OpenForRoom, NotifyRoomModeChanged; RoomBehaviors.OnRoomClicked
    ///
    /// View:        TowerView
    /// Systems:      Tower, RoomBehaviors (чтение + OnRoomClicked)
    /// Presenters:   RoomSelection, ProductionMode (вызывает)
    /// GameEvents:   RoomBuilt, GameTick, ProductionModeChanged, OperationModeUnlocked (слушает)
    /// </summary>
    public class TowerPresenter : MonoBehaviour
    {
        [SerializeField] private TowerView towerView;
        [SerializeField] private RoomSelectionPresenter roomSelection;
        [SerializeField] private ProductionModePresenter productionMode;

        private GameServices _services;
        private bool _towerEventsSubscribed;
        private bool _gameEventsSubscribed;

        public void Initialize(GameServices services)
        {
            _services = services;
            SubscribeTowerEvents();
            SubscribeGameEvents();
            RefreshTower();
            RefreshAllRoomOverlays();
        }

        private void OnDestroy()
        {
            UnsubscribeTowerEvents();
            UnsubscribeGameEvents();
        }

        public void RefreshTower()
        {
            if (towerView == null || _services == null)
                return;

            towerView.SyncFromState(_services.Tower);
        }

        public void RefreshAllRoomOverlays()
        {
            if (towerView == null || _services == null)
                return;

            var tower = _services.Tower;
            for (var i = 0; i < tower.Rooms.Count; i++)
            {
                var room = tower.Rooms[i];
                if (room.IsEmpty)
                    continue;

                var info = _services.RoomBehaviors.GetRoomStatusInfo(i);
                towerView.SetRoomStatus(i, info);
            }
        }

        public void RefreshRoomOverlay(int roomIndex)
        {
            if (towerView == null || _services == null)
                return;

            var info = _services.RoomBehaviors.GetRoomStatusInfo(roomIndex);
            towerView.SetRoomStatus(roomIndex, info);
        }

        private void HandleEmptyRoomClicked(int roomIndex)
        {
            roomSelection?.Open(roomIndex);
        }

        private void HandleRoomClicked(int roomIndex)
        {
            if (_services == null)
                return;

            var result = _services.RoomBehaviors.OnRoomClicked(roomIndex);
            RoomUiRouter.Open(this, result, roomIndex);
        }

        internal void OpenProductionMode(int roomIndex)
        {
            productionMode?.OpenForRoom(roomIndex);
        }

        private void OnRoomBuilt(int roomIndex, RoomDefinition room)
        {
            RefreshTower();
            RefreshRoomOverlay(roomIndex);
        }

        private void OnGameTick(TickContext context)
        {
            RefreshAllRoomOverlays();
        }

        private void OnProductionModeChanged(int roomIndex, ModeId modeId)
        {
            RefreshRoomOverlay(roomIndex);
            productionMode?.NotifyRoomModeChanged(roomIndex);
        }

        private void OnOperationModeUnlocked(int roomIndex, ModeId modeId)
        {
            RefreshRoomOverlay(roomIndex);
            productionMode?.NotifyRoomModeChanged(roomIndex);
        }

        private void SubscribeTowerEvents()
        {
            if (_towerEventsSubscribed || towerView == null)
                return;

            towerView.EmptyRoomClicked += HandleEmptyRoomClicked;
            towerView.RoomClicked += HandleRoomClicked;
            _towerEventsSubscribed = true;
        }

        private void UnsubscribeTowerEvents()
        {
            if (!_towerEventsSubscribed || towerView == null)
                return;

            towerView.EmptyRoomClicked -= HandleEmptyRoomClicked;
            towerView.RoomClicked -= HandleRoomClicked;
            _towerEventsSubscribed = false;
        }

        private void SubscribeGameEvents()
        {
            if (_gameEventsSubscribed)
                return;

            GameEvents.RoomBuilt += OnRoomBuilt;
            GameEvents.GameTick += OnGameTick;
            GameEvents.ProductionModeChanged += OnProductionModeChanged;
            GameEvents.OperationModeUnlocked += OnOperationModeUnlocked;
            _gameEventsSubscribed = true;
        }

        private void UnsubscribeGameEvents()
        {
            if (!_gameEventsSubscribed)
                return;

            GameEvents.RoomBuilt -= OnRoomBuilt;
            GameEvents.GameTick -= OnGameTick;
            GameEvents.ProductionModeChanged -= OnProductionModeChanged;
            GameEvents.OperationModeUnlocked -= OnOperationModeUnlocked;
            _gameEventsSubscribed = false;
        }
    }
}
