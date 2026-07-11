using System.Collections.Generic;
using IdleTower.Core;
using IdleTower.Data.Definitions;
using IdleTower.Systems;
using IdleTower.UI.Views;
using UnityEngine;

namespace IdleTower.UI.Presenters
{
    /// <summary>
    /// RoomSelectionPresenter — попап «что построить» в пустой слот (_pendingRoomIndex).
    ///
    /// Получает: вызов Open(roomIndex) от TowerPresenter; клик RoomSelectionPanel.RoomSelected
    /// Отправляет: RoomSelectionPanel.SetOptions, Show, Hide; Building.TryBuild
    ///
    /// View:        RoomSelectionPanel
    /// Systems:      UnlockTree (чтение), Building.CanAfford (чтение), Building.TryBuild (запись)
    /// Presenters:   — (вызывается из TowerPresenter)
    /// GameEvents:   —
    /// </summary>
    public class RoomSelectionPresenter : MonoBehaviour
    {
        [SerializeField] private RoomSelectionPanel panel;

        private GameServices _services;
        private int _pendingRoomIndex = -1;
        private bool _subscribed;

        public void Initialize(GameServices services)
        {
            _services = services;
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        public void Open(int roomIndex)
        {
            if (panel == null || _services == null)
                return;

            _pendingRoomIndex = roomIndex;
            RefreshOptions();
            panel.Show();
        }

        public void Close()
        {
            panel?.Hide();
            _pendingRoomIndex = -1;
        }

        private void RefreshOptions()
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
                displays.Add(new RoomOptionDisplay(room, canAfford));
            }

            panel.SetOptions(displays);
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

        private void Subscribe()
        {
            if (_subscribed || panel == null)
                return;

            panel.RoomSelected += HandleRoomSelected;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || panel == null)
                return;

            panel.RoomSelected -= HandleRoomSelected;
            _subscribed = false;
        }
    }
}
