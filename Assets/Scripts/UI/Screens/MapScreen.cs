using IdleTower.Core;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTower.UI.Screens
{
    /// <summary>
    /// Игровой экран карты (не общий UI). Sibling под Canvas рядом с UI_Root;
    /// Show/Hide переключает только этот root. Общий UI поверх — отдельно в UI_Root.
    /// </summary>
    public class MapScreen : MonoBehaviour, IScreen
    {
        [Tooltip("Корень экрана карты (обычно этот же GameObject).")]
        [SerializeField] private GameObject root;

        [SerializeField] private Button backToTowerButton;

        public ScreenId Id => ScreenId.Map;

        public event System.Action BackToTowerClicked;

        private void Awake()
        {
            if (root == null)
                root = gameObject;

            if (backToTowerButton != null)
                backToTowerButton.onClick.AddListener(HandleBackClick);
        }

        private void OnDestroy()
        {
            if (backToTowerButton != null)
                backToTowerButton.onClick.RemoveListener(HandleBackClick);
        }

        public void Show()
        {
            if (root != null)
                root.SetActive(true);
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }

        private void HandleBackClick()
        {
            BackToTowerClicked?.Invoke();
        }
    }
}
