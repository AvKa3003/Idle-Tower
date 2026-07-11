using System.Collections.Generic;
using IdleTower.Core;
using IdleTower.Core.Events;
using IdleTower.Data.Definitions;
using IdleTower.UI.Views;
using UnityEngine;

namespace IdleTower.UI.Presenters
{
    /// <summary>
    /// ResourceBarPresenter — панель ресурсов сверху экрана.
    ///
    /// Получает: GameEvents.ResourceChanged; количества из Wallet (чтение)
    /// Отправляет: ResourceBarPanel.SetResources / RefreshAmounts
    ///
    /// View:        ResourceBarPanel
    /// Systems:      Wallet (чтение), Balance.AllResources (fallback списка)
    /// Presenters:   —
    /// GameEvents:   ResourceChanged (слушает)
    /// </summary>
    public class ResourceBarPresenter : MonoBehaviour
    {
        [SerializeField] private ResourceBarPanel panel;
        [SerializeField] private ResourceDefinition[] trackedResources;

        private GameServices _services;
        private bool _subscribed;

        public void Initialize(GameServices services)
        {
            _services = services;
            Subscribe();
            RefreshAll();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        public void RefreshAll()
        {
            if (panel == null || _services == null)
                return;

            var resources = GetTrackedResources();
            panel.SetResources(resources, resource => _services.Wallet.GetAmount(resource));
        }

        private void OnResourceChanged(ResourceDefinition resource, int newAmount)
        {
            if (panel == null || _services == null)
                return;

            var resources = GetTrackedResources();
            panel.RefreshAmounts(resources, resource => _services.Wallet.GetAmount(resource));
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

        private void Subscribe()
        {
            if (_subscribed)
                return;

            GameEvents.ResourceChanged += OnResourceChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;

            GameEvents.ResourceChanged -= OnResourceChanged;
            _subscribed = false;
        }
    }
}
