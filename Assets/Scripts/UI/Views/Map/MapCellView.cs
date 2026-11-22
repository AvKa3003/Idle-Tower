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

        private Image _decal;

        [SerializeField] [Range(0f, 1f)] private float visibleOnlyDimAlpha = 0.75f;

        [Header("Сетка")]
        [SerializeField] private Vector2Int gridCoord;

        public Vector2Int Coord => gridCoord;

        public event System.Action<Vector2Int> Clicked;

        private MapView _mapView;

        private void Awake()
        {
            if (root == null)
                root = transform as RectTransform;

            if (button == null)
                button = GetComponentInChildren<Button>();

            _mapView = GetComponentInParent<MapView>();

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
            Sprite decal,
            MapPresence presence,
            bool hasFunctionalClick,
            bool canClick)
        {
            gridCoord = coord;
            gameObject.SetActive(true);

            var showDim = presence == MapPresence.VisibleOnly && hasFunctionalClick;
            var tint = showDim
                ? new Color(visibleOnlyDimAlpha, visibleOnlyDimAlpha, visibleOnlyDimAlpha, 1f)
                : Color.white;
            if (icon != null)
            {
                icon.enabled = sprite != null;
                icon.sprite = sprite;
                icon.color = tint;
            }

            ApplyDecal(decal, tint);
            ApplyIconToFootprint();

            // Затемнение VisibleOnly — цвет спрайта. Прямоугольник CanvasGroup не показывается.
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
                canvasGroup.gameObject.SetActive(false);
            }

            if (button != null)
            {
                // Картинка кнопки прозрачная: клик по ромбу, без заливки клетки.
                button.transition = Selectable.Transition.None;
                if (button.targetGraphic != null)
                {
                    var graphicColor = button.targetGraphic.color;
                    graphicColor.a = 0f;
                    button.targetGraphic.color = graphicColor;
                }

                button.interactable = canClick;
            }
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

        /// <summary>Декаль поверх спрайта клетки, клики не перехватывает.</summary>
        private void ApplyDecal(Sprite sprite, Color tint)
        {
            if (sprite == null)
            {
                if (_decal != null)
                    _decal.enabled = false;
                return;
            }

            var image = EnsureDecalImage();
            if (image == null)
                return;

            image.enabled = true;
            image.sprite = sprite;
            image.color = tint;
            image.raycastTarget = false;
        }

        private Image EnsureDecalImage()
        {
            if (_decal != null)
                return _decal;

            if (icon == null)
                return null;

            var go = new GameObject("Decal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(icon.transform.parent, false);
            go.transform.SetAsLastSibling();
            _decal = go.GetComponent<Image>();
            _decal.raycastTarget = false;
            return _decal;
        }

        /// <summary>
        /// Оригинальный размер спрайта; pivot из Texture совпадает с центром нижней грани footprint.
        /// </summary>
        private void ApplyIconToFootprint()
        {
            if (root == null)
                return;

            var footprintHeight = root.rect.height;
            PlaceOnFootprint(icon, footprintHeight);
            PlaceOnFootprint(_decal, footprintHeight);
        }

        private static void PlaceOnFootprint(Image image, float footprintHeight)
        {
            if (image == null)
                return;

            var sprite = image.sprite;
            if (sprite == null)
                return;

            var rt = image.rectTransform;
            var spriteW = sprite.rect.width;
            var spriteH = sprite.rect.height;
            if (spriteW <= 0f || spriteH <= 0f)
                return;

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.pivot = new Vector2(sprite.pivot.x / spriteW, sprite.pivot.y / spriteH);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, spriteW);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, spriteH);

            // Не вписываем в хитбокс — 1:1 px спрайта в UI-единицах (как baseWidth/Height).
            rt.anchoredPosition = new Vector2(0f, -footprintHeight * 0.5f);
        }

        private void HandleClick()
        {
            // Пан / pinch: не открывать клетку после сдвига.
            if (_mapView != null && _mapView.SuppressCellClicks)
                return;

            Clicked?.Invoke(Coord);
        }
    }
}
