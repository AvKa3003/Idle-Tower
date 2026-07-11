using System.Collections.Generic;
using IdleTower.Core;
using IdleTower.Systems;
using IdleTower.UI.Views;
using UnityEngine;

namespace IdleTower.UI.Presenters
{
    /// <summary>
    /// ProductionModePresenter — попап режимов производства (_roomIndex).
    ///
    /// Получает: OpenForRoom, NotifyRoomModeChanged от TowerPresenter; клики панели
    /// Отправляет: ProductionModePanel.SetOptions, Show, Hide; TryUnlockMode, TrySetMode
    ///
    /// View:        ProductionModePanel
    /// Systems:      RoomBehaviors (чтение + TryUnlockMode, TrySetMode)
    /// Presenters:   — (вызывается из TowerPresenter)
    /// GameEvents:   —
    /// </summary>
    public class ProductionModePresenter : MonoBehaviour
    {
        [SerializeField] private ProductionModePanel panel;

        private GameServices _services;
        private int _roomIndex = -1;
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

        public void OpenForRoom(int roomIndex)
        {
            if (panel == null || _services == null)
                return;

            _roomIndex = roomIndex;
            RefreshOptions();
            panel.Show();
        }

        public void Close()
        {
            panel?.Hide();
            _roomIndex = -1;
        }

        private void RefreshOptions()
        {
            if (_roomIndex < 0)
                return;

            var modes = _services.RoomBehaviors.GetOperationModes(_roomIndex);
            var displays = new List<OperationOptionDisplay>(modes.Count);

            for (var i = 0; i < modes.Count; i++)
            {
                var info = modes[i];
                var mode = info.Mode;
                if (mode == null)
                    continue;

                displays.Add(BuildDisplay(info));
            }

            panel.SetOptions(displays);
        }

        private static OperationOptionDisplay BuildDisplay(OperationModeInfo info)
        {
            var mode = info.Mode;
            var label = string.IsNullOrEmpty(mode.DisplayName) ? mode.Id : mode.DisplayName;

            if (info.IsUnlocked)
            {
                var detail = info.IsActive ? "Активен" : "Нажми «Выбрать»";
                return new OperationOptionDisplay(
                    info.ModeIndex,
                    label,
                    detail,
                    info.IsActive,
                    showUnlockButton: false,
                    showSelectButton: true,
                    interactable: !info.IsActive);
            }

            if (!info.RulesMet)
            {
                return new OperationOptionDisplay(
                    info.ModeIndex,
                    label,
                    "Условия не выполнены",
                    isActive: false,
                    showUnlockButton: false,
                    showSelectButton: false,
                    interactable: false);
            }

            var unlockCost = UiTextFormat.FormatCosts(mode.UnlockCost);
            var costLabel = string.IsNullOrEmpty(unlockCost) ? "Открыть" : $"Открыть: {unlockCost}";

            return new OperationOptionDisplay(
                info.ModeIndex,
                label,
                costLabel,
                isActive: false,
                showUnlockButton: true,
                showSelectButton: false,
                interactable: info.CanAffordUnlock);
        }

        private void HandleUnlockClicked(int modeIndex)
        {
            if (_services == null || _roomIndex < 0)
                return;

            _services.RoomBehaviors.TryUnlockMode(_roomIndex, modeIndex);
        }

        public void NotifyRoomModeChanged(int roomIndex)
        {
            if (_roomIndex == roomIndex)
                RefreshOptions();
        }

        private void HandleSelectClicked(int modeIndex)
        {
            if (_services == null || _roomIndex < 0)
                return;

            _services.RoomBehaviors.TrySetMode(_roomIndex, modeIndex);
        }

        private void Subscribe()
        {
            if (_subscribed || panel == null)
                return;

            panel.UnlockClicked += HandleUnlockClicked;
            panel.SelectClicked += HandleSelectClicked;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || panel == null)
                return;

            panel.UnlockClicked -= HandleUnlockClicked;
            panel.SelectClicked -= HandleSelectClicked;
            _subscribed = false;
        }
    }
}
