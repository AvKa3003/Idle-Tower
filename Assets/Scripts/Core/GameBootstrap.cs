using IdleTower.Data.Definitions;
using IdleTower.UI.Presenters;
using UnityEngine;

namespace IdleTower.Core
{
    /// <summary>
    /// Точка входа: валидация → GameServices → load/new game → UI.
    /// Автосейв по интервалу + pause / focus lost / quit.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        private const float DefaultAutoSaveIntervalSeconds = 60f;

        [SerializeField] private GameBalanceConfig balanceConfig;
        [SerializeField] private BuildingTreeConfig buildingTreeConfig;
        [SerializeField] private MainTowerPresenter mainTowerPresenter;

        [Tooltip("Интервал автосохранения в секундах (реальное время).")]
        [Min(5f)]
        [SerializeField] private float autoSaveIntervalSeconds = DefaultAutoSaveIntervalSeconds;

        private GameServices _services;
        private float _autoSaveTimer;
        private bool _bootstrapped;

        public GameServices Services => _services;

        private void Awake()
        {
            if (!TryBootstrap(out _services))
            {
                enabled = false;
                return;
            }

            _bootstrapped = true;
            _autoSaveTimer = 0f;
        }

        private void Update()
        {
            if (!_bootstrapped || _services == null)
                return;

            _services.TickSystem.ProcessUpdate(Time.deltaTime);

            _autoSaveTimer += Time.unscaledDeltaTime;
            if (_autoSaveTimer >= autoSaveIntervalSeconds)
            {
                _autoSaveTimer = 0f;
                _services.Save.TrySave(force: false);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                SaveNow("pause");
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                SaveNow("focus-lost");
        }

        private void OnApplicationQuit()
        {
            SaveNow("quit");
        }

        private void OnDestroy()
        {
            if (_bootstrapped)
                SaveNow("destroy");

            _services?.TickSystem.ClearTickables();
        }

        private void SaveNow(string reason)
        {
            if (!_bootstrapped || _services?.Save == null)
                return;

            // force: всегда писать при уходе; minInterval режет двойной pause+focus
            if (_services.Save.TrySave(force: true, minIntervalSeconds: 0.75f))
                Debug.Log($"[GameBootstrap] Save on {reason}");
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

            if (!services.Save.TryLoad())
                services.InitializeNewGame();

            mainTowerPresenter.Initialize(services);
            return true;
        }
    }
}
