using IdleTower.Data.Definitions;
using IdleTower.UI.Presenters;
using UnityEngine;

namespace IdleTower.Core
{
    /// <summary>
    /// Точка входа сцены: валидация конфигов → GameServices → тик → MainTowerPresenter.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameBalanceConfig balanceConfig;

        [SerializeField] private BuildingTreeConfig buildingTreeConfig;

        [SerializeField] private MainTowerPresenter mainTowerPresenter;

        private GameServices _services;

        public GameServices Services => _services;

        private void Awake()
        {
            if (!TryBootstrap(out _services))
                enabled = false;
        }

        private void Update()
        {
            _services?.TickSystem.ProcessUpdate(Time.deltaTime);
        }

        private void OnDestroy()
        {
            _services?.TickSystem.ClearTickables();
        }

        private bool TryBootstrap(out GameServices services)
        {
            services = null;

            if (balanceConfig == null || buildingTreeConfig == null)
            {
                Debug.LogError("[GameBootstrap] Назначьте GameBalanceConfig и BuildingTreeConfig в Inspector.");
                return false;
            }

            if (mainTowerPresenter == null)
            {
                Debug.LogError("[GameBootstrap] Назначьте MainTowerPresenter (UI_Root) в Inspector.");
                return false;
            }

            try
            {
                ConfigValidator.ValidateOrThrow(balanceConfig, buildingTreeConfig);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message, this);
                throw;
            }

            services = new GameServices(balanceConfig, buildingTreeConfig);
            services.InitializeNewGame();
            mainTowerPresenter.Initialize(services);
            return true;
        }
    }
}
