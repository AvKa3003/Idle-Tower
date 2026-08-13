using IdleTower.Map;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTower.UI.Views
{
    /// <summary>Одна клетка карты на UI-сетке. Без игровой логики.</summary>
    public class MapCellView : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Image icon;
        [SerializeField] private Button button;
        [SerializeField] private CanvasGroup canvasGroup;

        [SerializeField] [Range(0f, 1f)] private float visibleOnlyDimAlpha = 0.75f;

        [Header("Сетка")]
        [SerializeField] private Vector2Int gridCoord;

        public Vector2Int Coord => gridCoord;

        public event System.Action<Vector2Int> Clicked;

        private void Awake()
        {
            if (root == null)
                root = transform as RectTransform;

            if (button == null)
                button = GetComponentInChildren<Button>();

            if (button != null)
                button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }

        public void Setup(
            Vector2Int coord,
            Sprite sprite,
            MapPresence presence,
            bool hasFunctionalClick,
            bool canClick)
        {
            gridCoord = coord;
            gameObject.SetActive(true);

            if (icon != null)
                icon.sprite = sprite;

            var showDim = presence == MapPresence.VisibleOnly && hasFunctionalClick;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = showDim ? visibleOnlyDimAlpha : 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (button != null)
            {
                button.interactable = canClick;
            }
        }

        public void SetAnchoredPosition(Vector2 position)
        {
            if (root != null)
                root.anchoredPosition = position;
        }

        private void HandleClick()
        {
            Clicked?.Invoke(Coord);
        }
    }
}
