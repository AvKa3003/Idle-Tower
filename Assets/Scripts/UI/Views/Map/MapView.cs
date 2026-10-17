using System;
using System.Collections.Generic;
using IdleTower.Map;
using IdleTower.Systems;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Serialization;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace IdleTower.UI.Views
{
    /// <summary>
    /// Сетка клеток карты (изометрия). Только отображение — без Systems.
    /// Логические Coord не меняются; меняется только экранная раскладка.
    /// Пан / зум (колесо, pinch) — как в браузерных картах.
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
        [Tooltip("Масштаб всей сетки (CellsRoot). Не меняет baseWidth в px.")]
        [SerializeField] private float mapScale = 2f;

        [SerializeField] private float mapScaleMin = 0.5f;
        [SerializeField] private float mapScaleMax = 4f;

        [Header("Жесты (пан / зум)")]
        [Tooltip("Область жестов (обычно MapScreen). Пусто — родитель CellsRoot.")]
        [SerializeField] private RectTransform viewport;

        [Tooltip("Сдвиг в px экрана до пана (чтобы не съедать клик по клетке).")]
        [SerializeField] private float panDragThresholdPixels = 12f;

        [Tooltip("Чувствительность колеса (экспонента за единицу scroll).")]
        [SerializeField] private float wheelZoomSensitivity = 0.0015f;

        [Tooltip("Затухание инерции пана (выше — быстрее останавливается).")]
        [SerializeField] private float panInertiaDamping = 8f;

        [Tooltip("Порог скорости (parent-local units/s), ниже — инерция обнуляется.")]
        [SerializeField] private float panInertiaStopSpeed = 20f;

        private readonly List<MapCellView> _cells = new();
        private readonly List<Vector2> _pointerScreenBuffer = new(4);

        private Camera _uiCamera;
        private bool _suppressCellClicks;
        private bool _singlePanArmed;
        private Vector2 _singlePanScreenPos;
        private Vector2 _singlePanAccumScreen;
        private bool _pinchActive;
        private Vector2 _pinchMidScreen;
        private float _pinchDistance;
        private Vector2 _panVelocity;
        private bool _pointersWereDown;

        public float MapScale => mapScale;

        /// <summary>Пан/пинч превысили порог — клик по клетке в этом жесте игнорировать.</summary>
        public bool SuppressCellClicks => _suppressCellClicks;

        public event Action<Vector2Int> CellClicked;

        private void Awake()
        {
            ResolveRefs();
            ApplyMapScale();
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            ResolveRefs();
            CacheUiCamera();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            ResetGestureState(clearVelocity: true);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            mapScale = Mathf.Clamp(mapScale, mapScaleMin, mapScaleMax);
            if (!Application.isPlaying)
                ApplyMapScale();
        }
#endif

        private void Update()
        {
            if (cellsRoot == null)
                return;

            CacheUiCamera();
            GatherPointersOverViewport(_pointerScreenBuffer);

            var pointerCount = _pointerScreenBuffer.Count;
            if (pointerCount == 0)
            {
                if (_pointersWereDown)
                    ResetGestureState(clearVelocity: false);
                _pointersWereDown = false;
                ApplyPanInertia(Time.unscaledDeltaTime);
                HandleMouseWheelZoom();
                return;
            }

            _pointersWereDown = true;
            _panVelocity = Vector2.zero;

            if (pointerCount >= 2)
                UpdatePinchAndPan(_pointerScreenBuffer[0], _pointerScreenBuffer[1]);
            else
                UpdateSinglePointerPan(_pointerScreenBuffer[0]);

            HandleMouseWheelZoom();
        }

        /// <summary>Зум карты. Клетки в пространстве не пересчитываются — только scale корня.</summary>
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

        private void ResolveRefs()
        {
            if (cellsRoot == null)
                cellsRoot = transform as RectTransform;

            if (viewport == null && cellsRoot != null && cellsRoot.parent is RectTransform parentRt)
                viewport = parentRt;
        }

        private void CacheUiCamera()
        {
            var canvas = cellsRoot != null
                ? cellsRoot.GetComponentInParent<Canvas>()
                : GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                _uiCamera = null;
            else
                _uiCamera = canvas.worldCamera;
        }

        private RectTransform GestureParent
            => cellsRoot != null && cellsRoot.parent is RectTransform p ? p : viewport;

        private void GatherPointersOverViewport(List<Vector2> into)
        {
            into.Clear();

            if (Touch.activeTouches.Count > 0)
            {
                for (var i = 0; i < Touch.activeTouches.Count; i++)
                {
                    var touch = Touch.activeTouches[i];
                    if (!touch.isInProgress)
                        continue;

                    var screen = touch.screenPosition;
                    if (IsScreenPointInViewport(screen))
                        into.Add(screen);
                }

                return;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                var screen = mouse.position.ReadValue();
                if (IsScreenPointInViewport(screen))
                    into.Add(screen);
            }
        }

        private bool IsScreenPointInViewport(Vector2 screenPoint)
        {
            var area = viewport != null ? viewport : GestureParent;
            if (area == null)
                return true;

            return RectTransformUtility.RectangleContainsScreenPoint(area, screenPoint, _uiCamera);
        }

        private void UpdateSinglePointerPan(Vector2 screenPos)
        {
            // Переход с pinch: не прыгаем — берём текущую точку как новую базу.
            if (_pinchActive)
            {
                _pinchActive = false;
                _singlePanArmed = true;
                _singlePanScreenPos = screenPos;
                _singlePanAccumScreen = Vector2.zero;
                return;
            }

            if (!_singlePanArmed)
            {
                _singlePanArmed = true;
                _singlePanScreenPos = screenPos;
                _singlePanAccumScreen = Vector2.zero;
                return;
            }

            var screenFrom = _singlePanScreenPos;
            var screenDelta = screenPos - screenFrom;
            _singlePanScreenPos = screenPos;

            if (!_suppressCellClicks)
            {
                _singlePanAccumScreen += screenDelta;
                if (_singlePanAccumScreen.sqrMagnitude
                    < panDragThresholdPixels * panDragThresholdPixels)
                    return;

                _suppressCellClicks = true;
                screenFrom = screenPos - _singlePanAccumScreen;
                _singlePanAccumScreen = Vector2.zero;
            }

            if (TryScreenPointsToParentLocalDelta(screenFrom, screenPos, out var localDelta))
            {
                PanByParentLocal(localDelta);
                var dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
                _panVelocity = localDelta / dt;
            }
        }

        private void UpdatePinchAndPan(Vector2 screenA, Vector2 screenB)
        {
            // Переход с одного пальца на два.
            _singlePanArmed = false;
            _suppressCellClicks = true;

            var mid = (screenA + screenB) * 0.5f;
            var dist = Vector2.Distance(screenA, screenB);
            if (dist < 1f)
                dist = 1f;

            if (!_pinchActive)
            {
                _pinchActive = true;
                _pinchMidScreen = mid;
                _pinchDistance = dist;
                return;
            }

            var scaleFactor = dist / _pinchDistance;
            if (!Mathf.Approximately(scaleFactor, 1f))
                ZoomByFactorAroundScreenPoint(scaleFactor, mid);

            if (TryScreenPointsToParentLocalDelta(_pinchMidScreen, mid, out var localDelta))
            {
                PanByParentLocal(localDelta);
                var dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
                _panVelocity = localDelta / dt;
            }

            _pinchMidScreen = mid;
            _pinchDistance = dist;
        }

        private void HandleMouseWheelZoom()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f)
                return;

            var screen = mouse.position.ReadValue();
            if (!IsScreenPointInViewport(screen))
                return;

            // Пока pinch — только пальцы.
            if (_pinchActive)
                return;

            var factor = Mathf.Exp(scroll * wheelZoomSensitivity);
            ZoomByFactorAroundScreenPoint(factor, screen);
        }

        private void ZoomByFactorAroundScreenPoint(float factor, Vector2 screenPoint)
        {
            if (cellsRoot == null || GestureParent == null)
                return;

            var newScale = Mathf.Clamp(mapScale * factor, mapScaleMin, mapScaleMax);
            if (Mathf.Approximately(newScale, mapScale))
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    GestureParent, screenPoint, _uiCamera, out var focusParent))
                return;

            var oldScale = Mathf.Max(mapScale, 0.0001f);
            // Вектор от текущего смещения карты к центру жеста в координатах viewport, без учёта scale.
            var contentOffset = (focusParent - cellsRoot.anchoredPosition) / oldScale;

            mapScale = newScale;
            ApplyMapScale();
            // Та же точка контента под центром жеста, но уже в пропорциях нового scale.
            cellsRoot.anchoredPosition = focusParent - contentOffset * mapScale;
        }

        private void PanByParentLocal(Vector2 localDelta)
        {
            if (cellsRoot == null)
                return;

            cellsRoot.anchoredPosition += localDelta;
        }

        /// <summary>
        /// Для жестов: переводит две экранные точки в локаль viewport и возвращает вектор сдвига,
        /// если точка на viewport изменилась.
        /// </summary>
        private bool TryScreenPointsToParentLocalDelta(
            Vector2 screenFrom,
            Vector2 screenTo,
            out Vector2 localDelta)
        {
            localDelta = Vector2.zero;
            var parent = GestureParent;
            if (parent == null)
                return false;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent, screenFrom, _uiCamera, out var localFrom))
                return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent, screenTo, _uiCamera, out var localTo))
                return false;

            localDelta = localTo - localFrom;
            return localDelta.sqrMagnitude > 0.0000001f;
        }

        private void ApplyPanInertia(float dt)
        {
            if (cellsRoot == null || _panVelocity.sqrMagnitude < panInertiaStopSpeed * panInertiaStopSpeed)
            {
                _panVelocity = Vector2.zero;
                return;
            }

            cellsRoot.anchoredPosition += _panVelocity * dt;
            _panVelocity *= Mathf.Exp(-panInertiaDamping * dt);
        }

        private void ResetGestureState(bool clearVelocity)
        {
            _singlePanArmed = false;
            _singlePanAccumScreen = Vector2.zero;
            _pinchActive = false;
            _suppressCellClicks = false;
            if (clearVelocity)
                _panVelocity = Vector2.zero;
        }
    }
}
