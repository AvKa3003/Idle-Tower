using System;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTower.UI.Views
{
    public class HeaderPanel : MonoBehaviour
    {
        [SerializeField] private Button resourcesButton;
        [SerializeField] private Button unitsButton;
        [SerializeField] private Button mapButton;

        public event Action ResourcesClicked;
        public event Action UnitsClicked;
        public event Action MapClicked;

        private void Awake()
        {
            if (resourcesButton != null)
                resourcesButton.onClick.AddListener(HandleResourcesClick);

            if (unitsButton != null)
                unitsButton.onClick.AddListener(HandleUnitsClick);

            if (mapButton != null)
                mapButton.onClick.AddListener(HandleMapClick);
        }

        private void OnDestroy()
        {
            if (resourcesButton != null)
                resourcesButton.onClick.RemoveListener(HandleResourcesClick);

            if (unitsButton != null)
                unitsButton.onClick.RemoveListener(HandleUnitsClick);

            if (mapButton != null)
                mapButton.onClick.RemoveListener(HandleMapClick);
        }

        public void SetUnitsButtonVisible(bool visible)
        {
            if (unitsButton != null)
                unitsButton.gameObject.SetActive(visible);
        }

        private void HandleResourcesClick()
        {
            ResourcesClicked?.Invoke();
        }

        private void HandleUnitsClick()
        {
            UnitsClicked?.Invoke();
        }

        private void HandleMapClick()
        {
            MapClicked?.Invoke();
        }
    }
}
