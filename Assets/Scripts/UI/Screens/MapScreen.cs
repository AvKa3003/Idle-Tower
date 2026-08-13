using IdleTower.Core;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTower.UI.Screens
{
    public class MapScreen : MonoBehaviour, IScreen
    {
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
            root.SetActive(true);
        }

        public void Hide()
        {
            root.SetActive(false);
        }

        private void HandleBackClick()
        {
            BackToTowerClicked?.Invoke();
        }
    }
}
