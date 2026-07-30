using IdleTower.Core;
using IdleTower.UI.Views;
using UnityEngine;

namespace IdleTower.UI.Presenters
{
    /// <summary>
    /// HeaderPresenter — верхняя панель кнопок.
    ///
    /// Получает: клик HeaderPanel.ResourcesClicked; ссылки RoomSelection/ProductionMode от MainTowerPresenter
    /// Отправляет: Close чужих модалок; ResourceListPresenter.Open
    ///
    /// View:        HeaderPanel
    /// Systems:      —
    /// Presenters:   ResourceList (вызывает); RoomSelection, ProductionMode (закрывает)
    /// GameEvents:   —
    /// </summary>
    public class HeaderPresenter : MonoBehaviour
    {
        [SerializeField] private HeaderPanel panel;
        [SerializeField] private ResourceListPresenter resourceList;

        private RoomSelectionPresenter _roomSelection;
        private ProductionModePresenter _productionMode;
        private bool _subscribed;

        public void Initialize(
            GameServices services,
            RoomSelectionPresenter roomSelection,
            ProductionModePresenter productionMode)
        {
            _roomSelection = roomSelection;
            _productionMode = productionMode;
            resourceList?.Initialize(services);
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed || panel == null)
                return;

            panel.ResourcesClicked += HandleResourcesClicked;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || panel == null)
                return;

            panel.ResourcesClicked -= HandleResourcesClicked;
            _subscribed = false;
        }

        private void HandleResourcesClicked()
        {
            _roomSelection?.Close();
            _productionMode?.Close();
            resourceList?.Open();
        }
    }
}
