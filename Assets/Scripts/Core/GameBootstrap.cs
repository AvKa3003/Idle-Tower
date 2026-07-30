using IdleTower.Data.Definitions;
using IdleTower.UI.Presenters;
using UnityEngine;

namespace IdleTower.Core
{
    /// <summary>
    /// Точка входа сцены: конфиги → GameServices → тик → MainTowerPresenter.
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

            LogConfigWarnings(balanceConfig, buildingTreeConfig);

            services = new GameServices(balanceConfig, buildingTreeConfig);
            services.InitializeNewGame();
            mainTowerPresenter.Initialize(services);
            return true;
        }

        private static void LogConfigWarnings(GameBalanceConfig balance, BuildingTreeConfig buildingTree)
        {
            if (balance.AllResources == null || balance.AllResources.Length == 0)
                Debug.LogWarning("[GameBootstrap] GameBalanceConfig.AllResources пуст — модалка ресурсов будет пустой.");

            var rooms = buildingTree.AllRooms;
            if (rooms == null || rooms.Length == 0)
            {
                Debug.LogWarning("[GameBootstrap] BuildingTreeConfig.AllRooms пуст — в RoomSelection не будет вариантов.");
                return;
            }

            for (var i = 0; i < rooms.Length; i++)
            {
                var room = rooms[i];
                if (room == null)
                {
                    Debug.LogWarning($"[GameBootstrap] BuildingTreeConfig.AllRooms[{i}] = null.");
                    continue;
                }

                if (room.Behavior == null)
                    Debug.LogWarning($"[GameBootstrap] Room '{room.name}' без Behavior — постройка не сработает.");
            }
        }
    }
}
