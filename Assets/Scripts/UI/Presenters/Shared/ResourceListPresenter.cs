using System.Collections.Generic;
using IdleTower.Core;
using IdleTower.Core.Events;
using IdleTower.Data.Definitions;
using IdleTower.UI.Views;
using UnityEngine;

namespace IdleTower.UI.Presenters
{
    /// <summary>
    /// ResourceListPresenter — модалка списка ресурсов (!IsUnit), название + количество.
    ///
    /// Получает: Open/Close от HeaderPresenter; CloseClicked с панели; GameEvents.ResourceChanged
    /// Отправляет: ResourceListPanel.Open / RefreshRows, Hide
    ///
    /// View:        ResourceListPanel
    /// Systems:      Wallet (ключи = встреченные ресурсы)
    /// Presenters:   — (вызывается из HeaderPresenter)
    /// GameEvents:   ResourceChanged (слушает, пока попап открыт)
    /// </summary>
    public class ResourceListPresenter : MonoBehaviour
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
            var resources = CollectDiscoveredNonUnits();
            var displays = new List<ResourceListRowDisplay>(resources.Count);

            for (var i = 0; i < resources.Count; i++)
            {
                var resource = resources[i];
                var name = string.IsNullOrEmpty(resource.DisplayName)
                    ? resource.name
                    : resource.DisplayName;

                displays.Add(new ResourceListRowDisplay(
                    resource,
                    name,
                    resource.Icon,
                    _services.Wallet.GetAmount(resource)));
            }

            return displays;
        }

        private List<ResourceDefinition> CollectDiscoveredNonUnits()
        {
            var list = new List<ResourceDefinition>();
            var amounts = _services?.Wallet?.Amounts;
            if (amounts == null)
                return list;

            foreach (var pair in amounts)
            {
                var resource = pair.Key;
                if (resource == null || resource.IsUnit)
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

            if (resource != null && resource.IsUnit)
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
