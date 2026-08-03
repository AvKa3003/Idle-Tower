using System.Collections.Generic;
using IdleTower.Core;
using IdleTower.Core.Events;
using IdleTower.Data.Definitions;
using IdleTower.Rooms.Production;
using IdleTower.Systems;
using IdleTower.UI.Views;
using UnityEngine;

namespace IdleTower.UI.Presenters
{
    /// <summary>
    /// ProductionModePresenter — попап режимов производства (_roomIndex).
    ///
    /// Получает: OpenForRoom, NotifyRoomModeChanged от TowerPresenter; клики панели; GameEvents.ResourceChanged
    /// Отправляет: ProductionModePanel.Open / RefreshOptions, Hide; TryUnlockMode, TrySetMode, TryTogglePause
    ///
    /// View:        ProductionModePanel
    /// Systems:      Production (чтение + TryUnlockMode, TrySetMode)
    /// Presenters:   — (вызывается из TowerPresenter)
    /// GameEvents:   ResourceChanged (слушает, пока попап открыт)
    /// </summary>
    public class ProductionModePresenter : MonoBehaviour
    {
        [SerializeField] private ProductionModePanel panel;

        private GameServices _services;
        private int _roomIndex = -1;
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

        public void OpenForRoom(int roomIndex)
        {
            if (panel == null || _services == null)
                return;

            _roomIndex = roomIndex;
            panel.Open(BuildDisplays(), _services.Production.IsPaused(roomIndex));
        }

        public void Close()
        {
            panel?.Hide();
            _roomIndex = -1;
        }

        private void RefreshOptions()
        {
            if (_roomIndex < 0 || panel == null)
                return;

            panel.RefreshOptions(BuildDisplays());
            panel.SetPausedVisual(_services.Production.IsPaused(_roomIndex));
        }

        private List<OperationOptionDisplay> BuildDisplays()
        {
            var modes = _services.Production.GetOperationModes(_roomIndex);
            var displays = new List<OperationOptionDisplay>(modes.Count);

            for (var i = 0; i < modes.Count; i++)
            {
                var info = modes[i];
                var mode = info.Mode;
                if (mode == null)
                    continue;

                displays.Add(BuildDisplay(info));
            }

            return displays;
        }

        private OperationOptionDisplay BuildDisplay(OperationModeInfo info)
        {
            var mode = info.Mode;
            var label = string.IsNullOrEmpty(mode.DisplayName) ? mode.Id.Value : mode.DisplayName;
            var detail = BuildModeDetailText(mode, info);

            if (info.IsUnlocked)
            {
                return new OperationOptionDisplay(
                    info.ModeId,
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
                    info.ModeId,
                    label,
                    detail,
                    isActive: false,
                    showUnlockButton: false,
                    showSelectButton: false,
                    interactable: false);
            }

            return new OperationOptionDisplay(
                info.ModeId,
                label,
                detail,
                isActive: false,
                showUnlockButton: true,
                showSelectButton: false,
                interactable: info.CanAffordUnlock);
        }

        private string BuildModeDetailText(OperationMode mode, OperationModeInfo info)
        {
            if (info.IsUnlocked)
            {
                return ResourceTextFormat.FormatModeDetailWithBalance(
                    mode.InputPerCycle,
                    mode.OutputPerCycle,
                    mode.CycleDuration,
                    resource => _services.Wallet.GetAmount(resource));
            }

            if (!info.RulesMet)
                return "Условия не выполнены";

            var unlockCost = ResourceTextFormat.FormatCostsWithBalance(
                mode.UnlockCost,
                resource => _services.Wallet.GetAmount(resource));

            return string.IsNullOrEmpty(unlockCost)
                ? "Бесплатно"
                : $"Открыть: {unlockCost}";
        }

        private void HandleUnlockClicked(ModeId modeId)
        {
            if (_services == null || _roomIndex < 0)
                return;

            _services.Production.TryUnlockMode(_roomIndex, modeId);
        }

        public void NotifyRoomModeChanged(int roomIndex)
        {
            if (_roomIndex == roomIndex)
                RefreshOptions();
        }

        private void HandleSelectClicked(ModeId modeId)
        {
            if (_services == null || _roomIndex < 0)
                return;

            _services.Production.TrySetMode(_roomIndex, modeId);
        }

        private void HandlePauseClicked()
        {
            if (_services == null || _roomIndex < 0 || panel == null)
                return;

            if (!_services.Production.TryTogglePause(_roomIndex))
                return;

            panel.SetPausedVisual(_services.Production.IsPaused(_roomIndex));
        }

        private void OnResourceChanged(ResourceDefinition resource, int newAmount)
        {
            if (_roomIndex < 0)
                return;

            RefreshOptions();
        }

        private void SubscribePanel()
        {
            if (_panelSubscribed || panel == null)
                return;

            panel.UnlockClicked += HandleUnlockClicked;
            panel.SelectClicked += HandleSelectClicked;
            panel.CloseClicked += HandleCloseClicked;
            panel.PauseClicked += HandlePauseClicked;
            _panelSubscribed = true;
        }

        private void UnsubscribePanel()
        {
            if (!_panelSubscribed || panel == null)
                return;

            panel.UnlockClicked -= HandleUnlockClicked;
            panel.SelectClicked -= HandleSelectClicked;
            panel.CloseClicked -= HandleCloseClicked;
            panel.PauseClicked -= HandlePauseClicked;
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
