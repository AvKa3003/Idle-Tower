using IdleTower.Core;
using IdleTower.Map;
using IdleTower.UI.Screens;
using IdleTower.UI.Views;
using UnityEngine;

namespace IdleTower.UI.Presenters
{
    /// <summary>
    /// MapPresenter — экран карты.
    ///
    /// Получает: клики MapView / MapScreen / MapStubPanel
    /// Отправляет: MapSystem.TryClick; ScreenManager; панели / feedback по Action
    ///
    /// View:        MapScreen, MapView, MapStubPanel
    /// Systems:      Map
    /// GameEvents:   —
    /// </summary>
    public class MapPresenter : MonoBehaviour
    {
        [SerializeField] private MapScreen mapScreen;
        [SerializeField] private MapView mapView;
        [SerializeField] private MapStubPanel stubPanel;

        private GameServices _services;
        private ScreenManager _screenManager;
        private bool _subscribed;
        private bool _initialized;

        public void Initialize(GameServices services, ScreenManager screenManager)
        {
            if (services == null || _initialized)
                return;

            _services = services;
            _screenManager = screenManager;

            if (_screenManager != null && mapScreen != null)
                _screenManager.Register(mapScreen);

            Subscribe();
            RefreshView();
            _initialized = true;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        public void OpenMap()
        {
            stubPanel?.Hide();
            RefreshView();
            _screenManager?.Show(ScreenId.Map);
        }

        public void OpenMainTower()
        {
            stubPanel?.Hide();
            _screenManager?.Show(ScreenId.MainTower);
        }

        private void Subscribe()
        {
            if (_subscribed)
                return;

            if (mapView != null)
                mapView.CellClicked += HandleCellClicked;

            if (mapScreen != null)
                mapScreen.BackToTowerClicked += HandleBackToTowerClicked;

            if (stubPanel != null)
                stubPanel.Closed += HandleStubClosed;

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;

            if (mapView != null)
                mapView.CellClicked -= HandleCellClicked;

            if (mapScreen != null)
                mapScreen.BackToTowerClicked -= HandleBackToTowerClicked;

            if (stubPanel != null)
                stubPanel.Closed -= HandleStubClosed;

            _subscribed = false;
        }

        private void RefreshView()
        {
            if (mapView == null || _services?.Map == null)
                return;

            mapView.Sync(_services.Map.EnumerateDisplays());
        }

        private void HandleBackToTowerClicked()
        {
            OpenMainTower();
        }

        private void HandleStubClosed()
        {
        }

        private void HandleCellClicked(Vector2Int coord)
        {
            if (_services?.Map == null)
                return;

            var result = _services.Map.TryClick(coord);
            switch (result.Action)
            {
                case MapCellClickAction.GoToMainTower:
                    OpenMainTower();
                    break;

                case MapCellClickAction.OpenStub:
                    OpenStub(coord);
                    break;

                case MapCellClickAction.None:
                default:
                    break;
            }
        }

        private void OpenStub(Vector2Int coord)
        {
            var title = "Клетка";
            var body = "Заглушка. Рейд / лут появятся на следующих этапах.";

            if (_services.Map.TryGetCellDisplay(coord, out var info) && info.Definition != null)
            {
                if (!string.IsNullOrWhiteSpace(info.Definition.DisplayName))
                    title = info.Definition.DisplayName;
            }

            stubPanel?.Open(title, body);
        }
    }
}
