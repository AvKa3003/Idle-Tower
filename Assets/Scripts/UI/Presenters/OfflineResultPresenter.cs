using System.Collections.Generic;
using IdleTower.Core;
using IdleTower.Systems;
using IdleTower.UI.Views;
using UnityEngine;

namespace IdleTower.UI.Presenters
{
    /// <summary>
    /// OfflineResultPresenter — модалка итогов офлайн-догона после Load.
    ///
    /// Получает: TryShowFromLastCatchUp от MainTowerPresenter; CloseClicked с панели
    /// Отправляет: OfflineResultPanel.Open / Hide
    ///
    /// View:        OfflineResultPanel
    /// Systems:      OfflineSimulationSystem.LastResult (чтение)
    /// Presenters:   —
    /// GameEvents:   —
    /// </summary>
    public class OfflineResultPresenter : MonoBehaviour
    {
        [SerializeField] private OfflineResultPanel panel;

        private GameServices _services;
        private bool _panelSubscribed;

        public void Initialize(GameServices services)
        {
            _services = services;
            SubscribePanel();
            panel?.Hide();
        }

        private void OnDestroy()
        {
            UnsubscribePanel();
        }

        public void TryShowFromLastCatchUp()
        {
            if (panel == null || _services?.Offline == null)
                return;

            var result = _services.Offline.LastResult;
            if (result == null || !result.Applied)
                return;

            Open(result);
        }

        public void Open(OfflineCatchUpResult result)
        {
            if (panel == null || result == null || !result.Applied)
                return;

            BuildRowLists(result, out var resources, out var units);
            panel.Open(BuildSummary(result), resources, units);
        }

        public void Close()
        {
            panel?.Hide();
        }

        private static string BuildSummary(OfflineCatchUpResult result)
        {
            var away = ResourceTextFormat.FormatElapsedSeconds(result.RealElapsedSeconds);
            var credited = ResourceTextFormat.FormatElapsedSeconds(result.SimulatedSeconds);

            if (result.WasCapped)
            {
                return $"Вас не было: {away}\n" +
                       $"Начислено за: {credited} (лимит офлайна)";
            }

            return $"Вас не было: {away}\nНачислено за: {credited}";
        }

        private static void BuildRowLists(
            OfflineCatchUpResult result,
            out List<OfflineResultRowDisplay> resources,
            out List<OfflineResultRowDisplay> units)
        {
            var deltas = result.ResourceDeltas;
            resources = new List<OfflineResultRowDisplay>();
            units = new List<OfflineResultRowDisplay>();

            for (var i = 0; i < deltas.Count; i++)
            {
                var entry = deltas[i];
                var resource = entry.Resource;
                if (resource == null || entry.Delta == 0)
                    continue;

                var name = string.IsNullOrEmpty(resource.DisplayName)
                    ? resource.name
                    : resource.DisplayName;

                var display = new OfflineResultRowDisplay(
                    resource,
                    name,
                    resource.Icon,
                    entry.Delta);

                if (resource.IsUnit)
                    units.Add(display);
                else
                    resources.Add(display);
            }
        }

        private void SubscribePanel()
        {
            if (_panelSubscribed || panel == null)
                return;

            panel.CloseClicked += HandleCloseClicked;
            _panelSubscribed = true;
        }

        private void UnsubscribePanel()
        {
            if (!_panelSubscribed || panel == null)
                return;

            panel.CloseClicked -= HandleCloseClicked;
            _panelSubscribed = false;
        }

        private void HandleCloseClicked()
        {
            Close();
        }
    }
}
