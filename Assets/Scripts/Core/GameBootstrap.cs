using IdleTower.Data.Definitions;
using UnityEngine;

namespace IdleTower.Core
{
    /// <summary>Точка входа. Полный wiring презентеров — фаза 5.</summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameBalanceConfig balanceConfig;
        [SerializeField] private BuildingTreeConfig buildingTreeConfig;

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

            // IGameSystem регистрируются в фазе 2 (RoomBehaviorSystem и др.)
        }

        private void Update()
        {
            _services?.TickSystem.ProcessUpdate(Time.deltaTime);
        }

        private void OnDestroy()
        {
            _services?.TickSystem.ClearSystems();
        }
    }
}
