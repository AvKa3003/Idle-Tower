using System;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTower.UI.Views
{
    public class HeaderPanel : MonoBehaviour
    {
        [SerializeField] private Button resourcesButton;
        [SerializeField] private Button unitsButton;

        public event Action ResourcesClicked;
        public event Action UnitsClicked;

        private void Awake()
        {
            if (resourcesButton != null)
                resourcesButton.onClick.AddListener(HandleResourcesClick);

            if (unitsButton != null)
                unitsButton.onClick.AddListener(HandleUnitsClick);
        }

        private void OnDestroy()
        {
            if (resourcesButton != null)
                resourcesButton.onClick.RemoveListener(HandleResourcesClick);

            if (unitsButton != null)
                unitsButton.onClick.RemoveListener(HandleUnitsClick);
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
    }
}
