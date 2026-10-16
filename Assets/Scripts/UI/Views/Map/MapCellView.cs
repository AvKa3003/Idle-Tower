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

            ApplyIconToFootprint();

            var showDim = presence == MapPresence.VisibleOnly && hasFunctionalClick;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = showDim ? visibleOnlyDimAlpha : 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (button != null)
                button.interactable = canClick;
        }

        public void SetAnchoredPosition(Vector2 position)
        {
            if (root != null)
                root.anchoredPosition = position;
        }

        /// <summary>Размер hit-box / основания ромба; высокий спрайт может выходить за пределы.</summary>
        public void SetFootprintSize(Vector2 size)
        {
            if (root == null)
                return;

            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            ApplyIconToFootprint();
        }

        /// <summary>
        /// Оригинальный размер спрайта; pivot из Texture совпадает с центром нижней грани footprint.
        /// </summary>
        private void ApplyIconToFootprint()
        {
            if (icon == null || root == null)
                return;

            var sprite = icon.sprite;
            if (sprite == null)
                return;

            var iconRt = icon.rectTransform;
            var spriteW = sprite.rect.width;
            var spriteH = sprite.rect.height;
            if (spriteW <= 0f || spriteH <= 0f)
                return;

            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.localScale = Vector3.one;
            iconRt.pivot = new Vector2(sprite.pivot.x / spriteW, sprite.pivot.y / spriteH);
            iconRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, spriteW);
            iconRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, spriteH);

            // Не вписываем в хитбокс — 1:1 px спрайта в UI-единицах (как baseWidth/Height).
            var footprintHeight = root.rect.height;
            iconRt.anchoredPosition = new Vector2(0f, -footprintHeight * 0.5f);
        }

        private void HandleClick()
        {
            Clicked?.Invoke(Coord);
        }
    }
}
