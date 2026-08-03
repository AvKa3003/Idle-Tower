using System.Collections.Generic;
using IdleTower.Core;
using IdleTower.Core.Events;
using IdleTower.Data.Definitions;
using IdleTower.UI.Views;
using UnityEngine;

namespace IdleTower.UI.Presenters
{
    /// <summary>
    /// UnitListPresenter — модалка списка юнитов (IsUnit), название + количество.
    ///
    /// Получает: Open/Close от HeaderPresenter; CloseClicked с панели; GameEvents.ResourceChanged
    /// Отправляет: ResourceListPanel.Open / RefreshRows, Hide
    ///
    /// View:        ResourceListPanel (отдельный инстанс UnitListPanel в иерархии)
    /// Systems:      Wallet (ключи = встреченные юниты)
    /// Presenters:   — (вызывается из HeaderPresenter)
    /// GameEvents:   ResourceChanged (пока попап открыт)
    /// </summary>
    public class UnitListPresenter : MonoBehaviour
    {
        [SerializeField] private ResourceListPanel panel;

        private GameServices _services;
        private bool _isOpen;
        private bool _panelSubscribed;
        private bool _gameEventsSubscribed;

        public void Initialize(GameServices services)
        {
            _services = services;
            SubscribePanel();
            SubscribeGameEvents();
            panel?.Hide();
        }

        private void OnDestroy()
        {
            UnsubscribePanel();
            UnsubscribeGameEvents();
        }

        public void Open()
        {
            if (panel == null || _services == null)
                return;

            _isOpen = true;
            panel.Open(BuildDisplays());
        }

        public void Close()
        {
            _isOpen = false;
            panel?.Hide();
        }

        private void RefreshRows()
        {
            if (!_isOpen || panel == null || _services == null)
                return;

            panel.RefreshRows(BuildDisplays());
        }

        private List<ResourceListRowDisplay> BuildDisplays()
        {
            var units = CollectDiscoveredUnits();
            var displays = new List<ResourceListRowDisplay>(units.Count);

            for (var i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                var name = string.IsNullOrEmpty(unit.DisplayName)
                    ? unit.name
                    : unit.DisplayName;

                displays.Add(new ResourceListRowDisplay(
                    unit,
                    name,
                    unit.Icon,
                    _services.Wallet.GetAmount(unit)));
            }

            return displays;
        }

        private List<ResourceDefinition> CollectDiscoveredUnits()
        {
            var list = new List<ResourceDefinition>();
            var amounts = _services?.Wallet?.Amounts;
            if (amounts == null)
                return list;

            foreach (var pair in amounts)
            {
                var resource = pair.Key;
                if (resource == null || !resource.IsUnit)
                    continue;

                list.Add(resource);
            }

            list.Sort(CompareByDisplayName);
            return list;
        }

        private static int CompareByDisplayName(ResourceDefinition a, ResourceDefinition b)
        {
            var nameA = a != null && !string.IsNullOrEmpty(a.DisplayName) ? a.DisplayName : a?.name;
            var nameB = b != null && !string.IsNullOrEmpty(b.DisplayName) ? b.DisplayName : b?.name;
            return string.CompareOrdinal(nameA, nameB);
        }

        private void OnResourceChanged(ResourceDefinition resource, int newAmount)
        {
            if (!_isOpen)
                return;

            if (resource != null && !resource.IsUnit)
                return;

            RefreshRows();
        }

        private void SubscribePanel()
        {
            if (_panelSubscribed || panel == null)
                return;

            panel.CloseClicked += HandleCloseClicked;
            _panelSubscribed = true;
        }

        private void UnsubscribePanel()
        {
            if (!_panelSubscribed || panel == null)
                return;

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
