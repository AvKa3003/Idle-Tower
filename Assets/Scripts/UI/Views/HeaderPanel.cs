using System;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTower.UI.Views
{
    /// <summary>Верхняя панель с кнопками. Первая — список ресурсов.</summary>
    public class HeaderPanel : MonoBehaviour
    {
        [SerializeField] private Button resourcesButton;

        public event Action ResourcesClicked;

        private void Awake()
        {
            if (resourcesButton != null)
                resourcesButton.onClick.AddListener(HandleResourcesClick);
        }

        private void OnDestroy()
        {
            if (resourcesButton != null)
                resourcesButton.onClick.RemoveListener(HandleResourcesClick);
        }

        private void HandleResourcesClick()
        {
            ResourcesClicked?.Invoke();
        }
    }
}
