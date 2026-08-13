using IdleTower.Core;
using IdleTower.Core.Events;
using IdleTower.Data.Definitions;
using IdleTower.UI.Views;
using UnityEngine;

namespace IdleTower.UI.Presenters
{
    /// <summary>
    /// HeaderPresenter — верхняя панель кнопок.
    ///
    /// Получает: клики HeaderPanel (ресурсы / юниты / карта); ссылки RoomSelection/ProductionMode от UiRootPresenter
    /// Отправляет: Close чужих модалок; ResourceList / UnitList Open; MapPresenter.OpenMap
    ///
    /// View:        HeaderPanel
    /// Systems:      Wallet (видимость кнопки юнитов)
    /// Presenters:   ResourceList, UnitList, Map; RoomSelection, ProductionMode, OfflineResult (закрывает)
    /// GameEvents:   ResourceChanged (кнопка юнитов)
    /// </summary>
    public class HeaderPresenter : MonoBehaviour
    {
        [SerializeField] private HeaderPanel panel;
        [SerializeField] private ResourceListPresenter resourceList;
        [SerializeField] private UnitListPresenter unitList;

        private GameServices _services;
        private RoomSelectionPresenter _roomSelection;
        private ProductionModePresenter _productionMode;
        private OfflineResultPresenter _offlineResult;
        private MapPresenter _map;
        private bool _subscribed;
        private bool _gameEventsSubscribed;

        public void Initialize(
            GameServices services,
            RoomSelectionPresenter roomSelection,
            ProductionModePresenter productionMode,
            OfflineResultPresenter offlineResult = null,
            MapPresenter map = null)
        {
            _services = services;
            _roomSelection = roomSelection;
            _productionMode = productionMode;
            _offlineResult = offlineResult;
            _map = map;
            resourceList?.Initialize(services);
            unitList?.Initialize(services);
            Subscribe();
            SubscribeGameEvents();
            RefreshUnitsButtonVisibility();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            UnsubscribeGameEvents();
        }

        private void Subscribe()
        {
            if (_subscribed || panel == null)
                return;

            panel.ResourcesClicked += HandleResourcesClicked;
            panel.UnitsClicked += HandleUnitsClicked;
            panel.MapClicked += HandleMapClicked;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || panel == null)
                return;

            panel.ResourcesClicked -= HandleResourcesClicked;
            panel.UnitsClicked -= HandleUnitsClicked;
            panel.MapClicked -= HandleMapClicked;
            _subscribed = false;
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

        private void OnResourceChanged(ResourceDefinition resource, int newAmount)
        {
            if (resource == null || !resource.IsUnit)
                return;

            RefreshUnitsButtonVisibility();
        }

        private void RefreshUnitsButtonVisibility()
        {
            panel?.SetUnitsButtonVisible(HasDiscoveredUnit());
        }

        private bool HasDiscoveredUnit()
        {
            var amounts = _services?.Wallet?.Amounts;
            if (amounts == null)
                return false;

            foreach (var pair in amounts)
            {
                if (pair.Key != null && pair.Key.IsUnit)
                    return true;
            }

            return false;
        }

        private void CloseGameplayModals()
        {
            _roomSelection?.Close();
            _productionMode?.Close();
            _offlineResult?.Close();
        }

        private void HandleResourcesClicked()
        {
            CloseGameplayModals();
            unitList?.Close();
            resourceList?.Open();
        }

        private void HandleUnitsClicked()
        {
            CloseGameplayModals();
            resourceList?.Close();
            unitList?.Open();
        }

        private void HandleMapClicked()
        {
            CloseGameplayModals();
            resourceList?.Close();
            unitList?.Close();
            _map?.OpenMap();
        }
    }
}
