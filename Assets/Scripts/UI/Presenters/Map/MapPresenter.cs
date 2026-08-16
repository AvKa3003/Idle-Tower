using System.Collections.Generic;
using IdleTower.Core;
using IdleTower.Core.Events;
using IdleTower.Data.Definitions;
using IdleTower.Map;
using IdleTower.Map.Raid;
using IdleTower.Systems;
using IdleTower.UI.Screens;
using IdleTower.UI.Views;
using UnityEngine;

namespace IdleTower.UI.Presenters
{
    /// <summary>
    /// MapPresenter — экран карты + панель Raid.
    ///
    /// Получает: клики MapView / MapScreen / Raid (pause, close, ± армии);
    ///           OpenMap от Header; GameEvents (GameTick, ResourceChanged, MapPresenceChanged);
    ///           чтение Map (displays, RaidCellInfo)
    /// Отправляет: MapSystem.TryClick / TryTogglePause / TrySetPlannedArmyUnit;
    ///             ScreenManager Show Map/MainTower; MapView.Sync; Raid SetDisplay
    ///
    /// View:        MapScreen, MapView, MapRaidPanel
    /// Systems:      Map
    /// Presenters:   Header (вызывает OpenMap)
    /// GameEvents:   GameTick, ResourceChanged, MapPresenceChanged (слушает)
    /// </summary>
    public class MapPresenter : MonoBehaviour
    {
        [SerializeField] private MapScreen mapScreen;
        [SerializeField] private MapView mapView;
        [SerializeField] private MapRaidPanel raidPanel;

        private GameServices _services;
        private ScreenManager _screenManager;
        private Vector2Int _raidCoord;
        private bool _raidOpen;
        private bool _subscribed;
        private bool _gameEventsSubscribed;
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
            SubscribeGameEvents();
            RefreshView();
            _initialized = true;
        }

        private void OnDestroy()
        {
            Unsubscribe();
            UnsubscribeGameEvents();
        }

        public void OpenMap()
        {
            ClosePanels();
            RefreshView();
            _screenManager?.Show(ScreenId.Map);
        }

        public void OpenMainTower()
        {
            ClosePanels();
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

            if (raidPanel != null)
            {
                raidPanel.PauseClicked += HandleRaidPauseClicked;
                raidPanel.Closed += HandleRaidClosed;
                raidPanel.ArmyAmountDeltaClicked += HandleRaidArmyDelta;
            }

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

            if (raidPanel != null)
            {
                raidPanel.PauseClicked -= HandleRaidPauseClicked;
                raidPanel.Closed -= HandleRaidClosed;
                raidPanel.ArmyAmountDeltaClicked -= HandleRaidArmyDelta;
            }

            _subscribed = false;
        }

        private void SubscribeGameEvents()
        {
            if (_gameEventsSubscribed)
                return;

            GameEvents.GameTick += OnGameTick;
            GameEvents.ResourceChanged += OnResourceChanged;
            GameEvents.MapPresenceChanged += OnMapPresenceChanged;
            _gameEventsSubscribed = true;
        }

        private void UnsubscribeGameEvents()
        {
            if (!_gameEventsSubscribed)
                return;

            GameEvents.GameTick -= OnGameTick;
            GameEvents.ResourceChanged -= OnResourceChanged;
            GameEvents.MapPresenceChanged -= OnMapPresenceChanged;
            _gameEventsSubscribed = false;
        }

        private void OnGameTick(TickContext context)
        {
            if (_raidOpen)
                RefreshRaidPanel();
        }

        private void OnResourceChanged(ResourceDefinition resource, int amount)
        {
            if (_raidOpen)
                RefreshRaidPanel();
        }

        private void OnMapPresenceChanged()
        {
            RefreshView();
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

        private void HandleRaidClosed()
        {
            _raidOpen = false;
        }

        private void HandleRaidPauseClicked()
        {
            if (_services?.Map == null || !_raidOpen)
                return;

            if (_services.Map.TryTogglePause(_raidCoord))
                RefreshRaidPanel();
        }

        private void HandleRaidArmyDelta(ResourceDefinition unit, int delta)
        {
            if (_services?.Map == null || !_raidOpen || unit == null || delta == 0)
                return;

            if (!_services.Map.TryGetRaidInfo(_raidCoord, out var info))
                return;

            var current = RaidArmyHelper.GetAmount(info.PlannedArmy, unit);
            var next = Mathf.Max(0, current + delta);
            if (!_services.Map.TrySetPlannedArmyUnit(_raidCoord, unit, next))
                return;

            RefreshRaidPanel();
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

                case MapCellClickAction.OpenRaid:
                    OpenRaid(coord);
                    break;

                case MapCellClickAction.None:
                default:
                    break;
            }
        }

        private void OpenRaid(Vector2Int coord)
        {
            _raidCoord = coord;
            _raidOpen = true;
            raidPanel?.Open();
            RefreshRaidPanel();
        }

        private void RefreshRaidPanel()
        {
            if (raidPanel == null || _services?.Map == null || !_raidOpen)
                return;

            if (!_services.Map.TryGetRaidInfo(_raidCoord, out var info))
            {
                raidPanel.Hide();
                _raidOpen = false;
                return;
            }

            raidPanel.SetDisplay(BuildRaidDisplay(info));
            raidPanel.SetArmyRows(BuildArmyRows(info));
        }

        private MapRaidPanelDisplay BuildRaidDisplay(RaidCellInfo info)
        {
            var status = BuildStatus(info);
            var requirements = BuildRequirements(info);
            var rewards = "Награда за рейд:\n" + ResourceTextFormat.FormatCosts(info.Rewards);
            if (string.IsNullOrWhiteSpace(ResourceTextFormat.FormatCosts(info.Rewards)))
                rewards = "Награда за рейд: —";

            var progressLabel = info.HasActiveRaid
                ? $"{ResourceTextFormat.FormatElapsedSeconds(info.ElapsedSeconds)} / {ResourceTextFormat.FormatDuration(GameDuration.FromSeconds(info.DurationSeconds))}"
                : ResourceTextFormat.FormatDuration(GameDuration.FromSeconds(info.DurationSeconds));

            return new MapRaidPanelDisplay
            {
                Title = info.Title,
                Status = status,
                Requirements = requirements,
                Rewards = rewards,
                ProgressLabel = progressLabel,
                Progress01 = info.Progress01,
                IsPaused = info.IsPaused,
                PauseInteractable = info.Phase == RaidCellPhase.PreCapture
                    || (info.Phase == RaidCellPhase.Captured
                        && info.PostCaptureMode == PostCaptureMode.RaidFarm)
            };
        }

        private List<MapRaidArmyRowDisplay> BuildArmyRows(RaidCellInfo info)
        {
            var rows = new List<MapRaidArmyRowDisplay>();
            var all = _services.Balance?.AllResources;
            if (all == null)
                return rows;

            for (var i = 0; i < all.Length; i++)
            {
                var resource = all[i];
                if (resource == null || !resource.IsUnit)
                    continue;

                rows.Add(new MapRaidArmyRowDisplay
                {
                    Resource = resource,
                    Icon = resource.Icon,
                    Name = string.IsNullOrWhiteSpace(resource.DisplayName)
                        ? resource.name
                        : resource.DisplayName,
                    PlannedAmount = RaidArmyHelper.GetAmount(info.PlannedArmy, resource),
                    WalletAmount = _services.Wallet.GetAmount(resource),
                    StrengthPerUnit = resource.Strength
                });
            }

            return rows;
        }

        private static string BuildStatus(RaidCellInfo info)
        {
            if (info.Phase == RaidCellPhase.Captured
                && info.PostCaptureMode != PostCaptureMode.RaidFarm)
                return "Захвачено";

            if (info.HasActiveRaid)
                return info.IsPaused
                    ? $"Набег… (пауза новых) {info.CompletedRaids}/{info.MaxCompletedRaids}"
                    : $"Набег… {info.CompletedRaids}/{info.MaxCompletedRaids}";

            if (info.IsPaused)
                return $"Пауза {info.CompletedRaids}/{info.MaxCompletedRaids}";

            if (info.CanStartNow)
                return $"Готов к набегу {info.CompletedRaids}/{info.MaxCompletedRaids}";

            if (!info.MeetsRequirements)
                return $"Состав не подходит {info.CompletedRaids}/{info.MaxCompletedRaids}";

            return $"Ждём юнитов {info.CompletedRaids}/{info.MaxCompletedRaids}";
        }

        private string BuildRequirements(RaidCellInfo info)
        {
            var units = FormatRequiredUnitsForRaid(info);
            if (string.IsNullOrEmpty(units))
                units = "Обязательные юниты: —";
            else
                units = "Обязательные юниты:\n" + units;

            var strengthLine =
                $"Сила: нужно {info.RequiredStrength}, в составе {info.PlannedArmyStrength}"
                + (info.MeetsRequirements ? " (хватает)" : " (мало)");

            return units + "\n" + strengthLine;
        }

        /// <summary>Выбрано / нужно (в кошельке).</summary>
        private string FormatRequiredUnitsForRaid(RaidCellInfo info)
        {
            var required = info.RequiredUnits;
            if (required == null || required.Length == 0)
                return string.Empty;

            const string okColor = "#1B7A3D";
            const string badColor = "#E74C3C";
            var lines = new List<string>();

            for (var i = 0; i < required.Length; i++)
            {
                var cost = required[i];
                if (cost.Resource == null || cost.Amount <= 0)
                    continue;

                var selected = RaidArmyHelper.GetAmount(info.PlannedArmy, cost.Resource);
                var need = cost.Amount;
                var inWallet = _services.Wallet.GetAmount(cost.Resource);
                var name = string.IsNullOrWhiteSpace(cost.Resource.DisplayName)
                    ? cost.Resource.name
                    : cost.Resource.DisplayName;
                var color = selected >= need ? okColor : badColor;
                lines.Add($"<color={color}>{name} {selected}/{need} ({inWallet})</color>");
            }

            return string.Join("\n", lines);
        }

        private void ClosePanels()
        {
            raidPanel?.Hide();
            _raidOpen = false;
        }
    }
}
