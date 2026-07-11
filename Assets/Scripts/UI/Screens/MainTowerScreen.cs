using IdleTower.Core;
using UnityEngine;

namespace IdleTower.UI.Screens
{
    public class MainTowerScreen : MonoBehaviour, IScreen
    {
        [SerializeField] private GameObject root;

        public ScreenId Id => ScreenId.MainTower;

        private void Awake()
        {
            if (root == null)
                root = gameObject;
        }

        public void Show()
        {
            root.SetActive(true);
        }

        public void Hide()
        {
            root.SetActive(false);
        }
    }
}
