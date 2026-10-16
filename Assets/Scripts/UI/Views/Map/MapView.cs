using System;
using System.Collections.Generic;
using IdleTower.Map;
using IdleTower.Systems;
using UnityEngine;
using UnityEngine.Serialization;

namespace IdleTower.UI.Views
{
    /// <summary>
    /// Сетка клеток карты (изометрия). Только отображение — без Systems.
    /// Логические Coord не меняются; меняется только экранная раскладка.
    /// </summary>
    public class MapView : MonoBehaviour
    {
        [SerializeField] private RectTransform cellsRoot;
        [SerializeField] private MapCellView cellPrefab;
        [SerializeField] private Sprite fogSprite;

        [Header("Основание ромба (пиксели текстуры)")]
        [Tooltip("Ширина ромба основания клетки (= ширина спрайта земли в px).")]
        [FormerlySerializedAs("cellSize")]
        [SerializeField] private float baseWidth = 150f;

        [Tooltip("Высота ромба основания (= высота спрайта земли в px). Обычно baseWidth/2 для 2:1.")]
        [SerializeField] private float baseHeight = 75f;

        [Tooltip(
            "Зазор между основаниями в пикселях. 1 = 1px щель (целиком в half-step, без /2); 0 = вплотную.")]
        [SerializeField] private float spacingPixels = 1f;

        [Header("Подгонка шага (pixel-art)")]
        [Tooltip("Добавка к half-step по X: baseWidth/2 + spacing + adjust. Для 64px обычно 0.")]
        [SerializeField] private float stepAdjustX = 0f;

        [Tooltip(
            "Добавка к half-step по Y: baseHeight/2 + spacing + adjust. " +
            "1 → шаг 17 при baseHeight 32 (стык «ниже-правее»); подстрой глазами.")]
        [SerializeField] private float stepAdjustY = 1f;

        [Header("Масштаб")]
        [Tooltip("Масштаб всей сетки (CellsRoot). Позже — колесо мыши / pinch. Не меняет baseWidth в px.")]
        [SerializeField] private float mapScale = 2f;

        [SerializeField] private float mapScaleMin = 0.5f;
        [SerializeField] private float mapScaleMax = 4f;

        private readonly List<MapCellView> _cells = new();

        public float MapScale => mapScale;

        public event Action<Vector2Int> CellClicked;

        private void Awake()
        {
            ApplyMapScale();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            mapScale = Mathf.Clamp(mapScale, mapScaleMin, mapScaleMax);
            if (!Application.isPlaying)
                ApplyMapScale();
        }
#endif

        /// <summary>Зум карты (для колеса / свайпа). Клетки в пространстве не пересчитываются — только scale корня.</summary>
        public void SetMapScale(float scale)
        {
            mapScale = Mathf.Clamp(scale, mapScaleMin, mapScaleMax);
            ApplyMapScale();
        }

        public void Sync(IEnumerable<MapCellDisplayInfo> displays)
        {
            Clear();

            if (displays == null || cellPrefab == null)
                return;

            ApplyMapScale();

            var parent = cellsRoot != null ? cellsRoot : transform as RectTransform;
            // spacingPixels целиком в half-step: 1 = 1px щель между основаниями (не /2).
            var halfStepX = Mathf.Max(0f, baseWidth * 0.5f + spacingPixels + stepAdjustX);
            var halfStepY = Mathf.Max(0f, baseHeight * 0.5f + spacingPixels + stepAdjustY);
            var footprint = new Vector2(baseWidth, baseHeight);

            foreach (var info in displays)
            {
                var isFog = info.Presence == MapPresence.Fog;
                var sprite = isFog
                    ? fogSprite
                    : (info.Sprite != null ? info.Sprite : info.Definition.Sprite);

                var cell = Instantiate(cellPrefab, parent);
                cell.SetFootprintSize(footprint);
                cell.Setup(
                    info.Coord,
                    sprite,
                    info.Presence,
                    info.HasFunctionalClick,
                    !isFog && info.CanClick);
                cell.SetAnchoredPosition(CoordToAnchored(info.Coord, halfStepX, halfStepY));
                cell.Clicked += HandleCellClicked;
                _cells.Add(cell);
            }

            ApplyDepthSort();
        }

        public void Clear()
        {
            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                if (cell == null)
                    continue;

                cell.Clicked -= HandleCellClicked;
                Destroy(cell.gameObject);
            }

            _cells.Clear();
        }

        /// <summary>
        /// Изометрия: X вправо-вниз по сетке, Y влево-вниз (UI Y вверх).
        /// spacingPixels добавляется целиком к half-step (1 = 1px щель); stepAdjust* — ручная подгонка.
        /// </summary>
        private static Vector2 CoordToAnchored(Vector2Int coord, float halfStepX, float halfStepY)
        {
            return new Vector2(
                (coord.x - coord.y) * halfStepX,
                (coord.x + coord.y) * halfStepY);
        }

        /// <summary>
        /// Дальние клетки (больший x+y) рисуются раньше; ближние — сверху (перекрытия крон/башен).
        /// </summary>
        private void ApplyDepthSort()
        {
            _cells.Sort(CompareDepthBackToFront);
            for (var i = 0; i < _cells.Count; i++)
            {
                if (_cells[i] != null)
                    _cells[i].transform.SetSiblingIndex(i);
            }
        }

        private static int CompareDepthBackToFront(MapCellView a, MapCellView b)
        {
            if (ReferenceEquals(a, b))
                return 0;
            if (a == null)
                return 1;
            if (b == null)
                return -1;

            // Больший x+y = дальше «на север» экрана = позади.
            var depthA = a.Coord.x + a.Coord.y;
            var depthB = b.Coord.x + b.Coord.y;
            var byDepth = depthB.CompareTo(depthA);
            if (byDepth != 0)
                return byDepth;

            // Стабильный тай-брейк.
            byDepth = a.Coord.x.CompareTo(b.Coord.x);
            return byDepth != 0 ? byDepth : a.Coord.y.CompareTo(b.Coord.y);
        }

        private void ApplyMapScale()
        {
            var target = cellsRoot != null ? cellsRoot : transform as RectTransform;
            if (target == null)
                return;

            var s = Mathf.Clamp(mapScale, mapScaleMin, mapScaleMax);
            target.localScale = new Vector3(s, s, 1f);
        }

        private void HandleCellClicked(Vector2Int coord)
        {
            CellClicked?.Invoke(coord);
        }
    }
}
