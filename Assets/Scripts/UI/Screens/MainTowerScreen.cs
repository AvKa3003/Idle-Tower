using IdleTower.Core;
using UnityEngine;

namespace IdleTower.UI.Screens
{
    /// <summary>
    /// Игровой экран башни (не UI_Root). Обычно на GO Tower; root — этот же объект.
    /// </summary>
    public class MainTowerScreen : MonoBehaviour, IScreen
    {
        [Tooltip("Корень экрана башни (обычно GO Tower). Не UI_Root.")]
        [SerializeField] private GameObject root;

        public ScreenId Id => ScreenId.MainTower;

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
    }
}
