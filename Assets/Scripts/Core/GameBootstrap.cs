using IdleTower.Data.Definitions;
using IdleTower.UI.Presenters;
using UnityEngine;

namespace IdleTower.Core
{
    /// <summary>Точка входа. Presenters инициализируются через MainTowerPresenter.</summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameBalanceConfig balanceConfig;
        [SerializeField] private BuildingTreeConfig buildingTreeConfig;
        [SerializeField] private MainTowerPresenter mainTowerPresenter;
        private GameServices _services;

        public GameServices Services => _services;

        private void Awake()
        {
            if (balanceConfig == null || buildingTreeConfig == null)
            {
                Debug.LogError("[GameBootstrap] Назначьте GameBalanceConfig и BuildingTreeConfig в Inspector.");
                enabled = false;
                return;
            }

            _services = new GameServices(balanceConfig, buildingTreeConfig);
            _services.InitializeNewGame();
            mainTowerPresenter?.Initialize(_services);
        }

        private void Update()
        {
            _services?.TickSystem.ProcessUpdate(Time.deltaTime);
        }

        private void OnDestroy()
        {
            _services?.TickSystem.ClearTickables();
        }
    }
}
