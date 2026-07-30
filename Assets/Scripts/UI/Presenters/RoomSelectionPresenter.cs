using System.Collections.Generic;
using IdleTower.Core;
using IdleTower.Core.Events;
using IdleTower.Data.Definitions;
using IdleTower.Systems;
using IdleTower.UI.Views;
using UnityEngine;

namespace IdleTower.UI.Presenters
{
    /// <summary>
    /// RoomSelectionPresenter — попап «что построить» в пустой слот (_pendingRoomIndex).
    ///
    /// Получает: вызов Open(roomIndex) от TowerPresenter; клик RoomSelectionPanel; GameEvents.ResourceChanged
    /// Отправляет: RoomSelectionPanel.Open / RefreshOptions, Hide; Building.TryBuild
    ///
    /// View:        RoomSelectionPanel
    /// Systems:      UnlockTree (чтение), Building.CanAfford (чтение), Building.TryBuild (запись)
    /// Presenters:   — (вызывается из TowerPresenter)
    /// GameEvents:   ResourceChanged (слушает, пока попап открыт)
    /// </summary>
    public class RoomSelectionPresenter : MonoBehaviour
    {
        [SerializeField] private RoomSelectionPanel panel;

        private GameServices _services;
        private int _pendingRoomIndex = -1;
        private bool _panelSubscribed;
        private bool _gameEventsSubscribed;

        public void Initialize(GameServices services)
        {
            _services = services;
            SubscribePanel();
            SubscribeGameEvents();
        }

        private void OnDestroy()
        {
            UnsubscribePanel();
            UnsubscribeGameEvents();
        }

        public void Open(int roomIndex)
        {
            if (panel == null || _services == null)
                return;

            _pendingRoomIndex = roomIndex;
            panel.Open(BuildDisplays());
        }

        public void Close()
        {
            panel?.Hide();
            _pendingRoomIndex = -1;
        }

        private void RefreshOptions()
        {
            if (_pendingRoomIndex < 0)
                return;

            panel.RefreshOptions(BuildDisplays());
        }

        private List<RoomOptionDisplay> BuildDisplays()
        {
            var tower = _services.Tower;
            var available = _services.UnlockTree.GetAvailableRooms(tower);
            var displays = new List<RoomOptionDisplay>(available.Count);

            for (var i = 0; i < available.Count; i++)
            {
                var room = available[i];
                if (room == null)
                    continue;

                var canAfford = _services.Building.CanAfford(room);
                var costText = BuildCostText(room.Cost);
                displays.Add(new RoomOptionDisplay(room, canAfford, costText));
            }

            return displays;
        }

        private string BuildCostText(ResourceCost[] cost)
        {
            var costLabel = ResourceTextFormat.FormatCostsWithBalance(
                cost,
                resource => _services.Wallet.GetAmount(resource));

            return string.IsNullOrEmpty(costLabel) ? "Бесплатно" : costLabel;
        }

        private void HandleRoomSelected(RoomDefinition room)
        {
            if (_services == null || room == null || _pendingRoomIndex < 0)
                return;

            var result = _services.Building.TryBuild(_pendingRoomIndex, room);
            if (result == BuildResult.Success)
                Close();
            else
                Debug.Log($"[RoomSelection] TryBuild failed: {result}");
        }

        private void OnResourceChanged(ResourceDefinition resource, int newAmount)
        {
            if (_pendingRoomIndex < 0)
                return;

            RefreshOptions();
        }

        private void SubscribePanel()
        {
            if (_panelSubscribed || panel == null)
                return;

            panel.RoomSelected += HandleRoomSelected;
            panel.CloseClicked += HandleCloseClicked;
            _panelSubscribed = true;
        }

        private void UnsubscribePanel()
        {
            if (!_panelSubscribed || panel == null)
                return;

            panel.RoomSelected -= HandleRoomSelected;
            panel.CloseClicked -= HandleCloseClicked;
            _panelSubscribed = false;
        }

        private void SubscribeGameEvents()
        {
            if (_gameEventsSubscribed)
                return;

            GameEvents.ResourceChanged += OnResourceChanged;
            _gameEventsSubscribed = true;
        }

        private void UnsubscribeGameEvents()
        {
            if (!_gameEventsSubscribed)
                return;

            GameEvents.ResourceChanged -= OnResourceChanged;
            _gameEventsSubscribed = false;
        }

        private void HandleCloseClicked()
        {
            Close();
        }
    }
}
