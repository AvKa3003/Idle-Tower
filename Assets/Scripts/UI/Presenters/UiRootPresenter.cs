using IdleTower.Core;
using IdleTower.UI.Screens;
using UnityEngine;

namespace IdleTower.UI.Presenters
{
    /// <summary>
    /// UiRootPresenter — общий bootstrap UI (GO UI_Root).
    /// Регистрирует экраны, инициализирует presenters, стартовый Show.
    ///
    /// Не логика башни: за башню отвечают MainTowerScreen + TowerPresenter (GO Tower).
    ///
    /// Получает: GameServices от GameBootstrap
    /// Отправляет: Initialize(services) дочерним presenters; Show MainTowerScreen
    ///
    /// Screens:      MainTowerScreen, MapScreen (через MapPresenter)
    /// Presenters:   Header, RoomSelection, ProductionMode, Tower, OfflineResult, Map
    /// GameEvents:   —
    /// </summary>
    public class UiRootPresenter : MonoBehaviour
    {
        [SerializeField] private ScreenManager screenManager;
        [SerializeField] private MainTowerScreen mainTowerScreen;
        [SerializeField] private HeaderPresenter header;
        [SerializeField] private RoomSelectionPresenter roomSelection;
        [SerializeField] private ProductionModePresenter productionMode;
        [SerializeField] private TowerPresenter tower;
        [SerializeField] private OfflineResultPresenter offlineResult;
        [SerializeField] private MapPresenter map;

        private bool _initialized;

        public void Initialize(GameServices services)
        {
            if (services == null || _initialized)
                return;

            if (screenManager != null && mainTowerScreen != null)
                screenManager.Register(mainTowerScreen);

            roomSelection?.Initialize(services);
            productionMode?.Initialize(services);
            offlineResult?.Initialize(services);
            map?.Initialize(services, screenManager);
            header?.Initialize(services, roomSelection, productionMode, offlineResult, map);
            tower?.Initialize(services);

            if (screenManager != null)
                screenManager.Show(ScreenId.MainTower);
            else
                mainTowerScreen?.Show();

            offlineResult?.TryShowFromLastCatchUp();

            _initialized = true;
        }
    }
}
