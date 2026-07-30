using System.Collections.Generic;
using IdleTower.Core;
using IdleTower.Core.Events;
using IdleTower.Data.Definitions;
using IdleTower.UI.Views;
using UnityEngine;

namespace IdleTower.UI.Presenters
{
    /// <summary>
    /// ResourceListPresenter — модалка списка ресурсов (название + количество).
    ///
    /// Получает: Open/Close от HeaderPresenter; CloseClicked с панели; GameEvents.ResourceChanged
    /// Отправляет: ResourceListPanel.Open / RefreshRows, Hide
    ///
    /// View:        ResourceListPanel
    /// Systems:      Wallet (чтение), Balance.AllResources (список)
    /// Presenters:   — (вызывается из HeaderPresenter)
    /// GameEvents:   ResourceChanged (слушает, пока попап открыт)
    /// </summary>
    public class ResourceListPresenter : MonoBehaviour
    {
        [SerializeField] private ResourceListPanel panel;
        [SerializeField] private ResourceDefinition[] trackedResources;

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
            var resources = GetTrackedResources();
            var displays = new List<ResourceListRowDisplay>(resources.Count);

            for (var i = 0; i < resources.Count; i++)
            {
                var resource = resources[i];
                if (resource == null)
                    continue;

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

        private IReadOnlyList<ResourceDefinition> GetTrackedResources()
        {
            if (trackedResources != null && trackedResources.Length > 0)
                return trackedResources;

            return CollectResourcesFromBalance();
        }

        private List<ResourceDefinition> CollectResourcesFromBalance()
        {
            var list = new List<ResourceDefinition>();
            var balance = _services?.Balance;
            if (balance?.AllResources == null)
                return list;

            foreach (var resource in balance.AllResources)
            {
                if (resource != null && !list.Contains(resource))
                    list.Add(resource);
            }

            return list;
        }

        private void OnResourceChanged(ResourceDefinition resource, int newAmount)
        {
            if (!_isOpen)
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
