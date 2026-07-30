using IdleTower.Core;
using IdleTower.UI.Screens;
using UnityEngine;

namespace IdleTower.UI.Presenters
{
    /// <summary>
    /// MainTowerPresenter — bootstrap главного экрана.
    ///
    /// Получает: GameServices от GameBootstrap
    /// Отправляет: Initialize(services) дочерним presenters; Show MainTowerScreen
    ///
    /// View:        MainTowerScreen
    /// Systems:      —
    /// Presenters:   Header, RoomSelection, ProductionMode, Tower
    /// GameEvents:   —
    /// </summary>
    public class MainTowerPresenter : MonoBehaviour
    {
        [SerializeField] private ScreenManager screenManager;
        [SerializeField] private MainTowerScreen mainTowerScreen;
        [SerializeField] private HeaderPresenter header;
        [SerializeField] private RoomSelectionPresenter roomSelection;
        [SerializeField] private ProductionModePresenter productionMode;
        [SerializeField] private TowerPresenter tower;

        private bool _initialized;

        public void Initialize(GameServices services)
        {
            if (services == null || _initialized)
                return;

            if (screenManager != null && mainTowerScreen != null)
                screenManager.Register(mainTowerScreen);

            roomSelection?.Initialize(services);
            productionMode?.Initialize(services);
            header?.Initialize(services, roomSelection, productionMode);
            tower?.Initialize(services);

            if (screenManager != null)
                screenManager.Show(ScreenId.MainTower);
            else
                mainTowerScreen?.Show();

            _initialized = true;
        }
    }
}
