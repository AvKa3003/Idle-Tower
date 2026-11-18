using System.Collections.Generic;
using IdleTower.Data.Definitions;
using IdleTower.Map;
using IdleTower.Map.Behaviors;
using UnityEditor;
using UnityEngine;

namespace IdleTower.Editor
{
    /// <summary>
    /// Редактор MapConfig: iso-превью, dirty/Save, смена карты,
    /// Paint / Erase / Move. Inspector selection — этап 5.
    /// </summary>
    public sealed class MapEditorWindow : EditorWindow
    {
        private enum MapEditTool
        {
            Select,
            Paint,
            Erase,
            Move
        }

        private const string MenuPath = "IdleTower/Map Editor";

        // Как на MapView в сцене (SampleScene): 64×32, spacing 0, adjustY 1, scale 4.
        private float _baseWidth = 64f;
        private float _baseHeight = 32f;
        private float _spacingPixels;
        private float _stepAdjustX;
        private float _stepAdjustY = 1f;
        private float _zoom = 4f;
        private Vector2 _pan;
        private bool _isPanning;
        private Vector2 _panStartMouse;
        private Vector2 _panStartOffset;

        private MapConfig _config;
        private SerializedObject _serializedConfig;
        private bool _sessionDirty;
        private string _selectedInstanceId = string.Empty;
        private Vector2Int? _hoverCoord;
        private bool _hoverOccupied;
        private bool _layoutFoldout;
        private MapEditTool _tool = MapEditTool.Select;
        private MapCellDefinition _paintDefinition;
        private bool _strokeActive;
        private Vector2Int? _lastStrokeCoord;
        private string _statusHint = string.Empty;

        private readonly List<PreviewCell> _drawBuffer = new(128);
        private readonly Dictionary<Vector2Int, int> _coordToEntry = new();

        private struct PreviewCell
        {
            public Vector2Int Coord;
            public string InstanceId;
            public Sprite Sprite;
            public Vector2 ContentPos;
        }

        [MenuItem(MenuPath)]
        public static void Open()
        {
            var window = GetWindow<MapEditorWindow>();
            window.titleContent = new GUIContent("Map Editor");
            window.minSize = new Vector2(640f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            autoRepaintOnSceneChange = true;
            SyncUnsavedFlag();
        }

        private void OnDisable()
        {
            // Закрытие окна: Unity спросит через hasUnsavedChanges + SaveChanges/DiscardChanges.
        }

        public override void SaveChanges()
        {
            SaveAsset();
            base.SaveChanges();
        }

        public override void DiscardChanges()
        {
            ReloadFromDisk();
            base.DiscardChanges();
        }

        private void OnGUI()
        {
            HandleToolHotkeys();
            DrawToolbar();
            EditorGUILayout.Space(4f);

            var previewRect = GUILayoutUtility.GetRect(
                GUIContent.none,
                GUIStyle.none,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            DrawPreview(previewRect);
            DrawStatusBar();

            if (Event.current.type == EventType.MouseMove)
                Repaint();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();
                var next = (MapConfig)EditorGUILayout.ObjectField(
                    _config,
                    typeof(MapConfig),
                    false,
                    GUILayout.MinWidth(180f));
                if (EditorGUI.EndChangeCheck())
                    TryBindConfig(next);

                DrawToolToggle(MapEditTool.Select, "Select", "ЛКМ: выбрать клетку. Горячая клавиша 1.");
                DrawToolToggle(MapEditTool.Paint, "Paint", "ЛКМ / drag: поставить или заменить Cell. Горячая клавиша 2.");
                DrawToolToggle(MapEditTool.Erase, "Erase", "ЛКМ / drag: удалить клетку. Горячая клавиша 3.");
                DrawToolToggle(MapEditTool.Move, "Move", "ЛКМ по клетке → ЛКМ по пустому coord. InstanceId не меняется. Горячая клавиша 4.");

                GUILayout.FlexibleSpace();

                var shortcutsContent = new GUIContent(
                    "⇧ Shortcuts",
                    "Превью карты:\n" +
                    "• Пан (камера): СКМ (средняя кнопка) или Alt + ЛКМ\n" +
                    "• Зум: колёсико мыши (к курсору)\n" +
                    "• Select / Paint / Erase / Move: 1–4\n" +
                    "• Select: ЛКМ по клетке\n" +
                    "• Paint: ЛКМ / drag (нужен Brush)\n" +
                    "• Erase: ЛКМ / drag\n" +
                    "• Move: ЛКМ pick → ЛКМ пустой coord (занято — запрет)\n" +
                    "• Esc: снять select\n" +
                    "• Drag MapCellDefinition на coord: add / replace Cell\n" +
                    "• Назначить карту: ObjectField или drag MapConfig в окно\n" +
                    "• Сброс пана: кнопка в «Превью: шаг / зум»");
                GUILayout.Label(shortcutsContent, EditorStyles.toolbarButton);

                using (new EditorGUI.DisabledScope(_config == null || !_sessionDirty))
                {
                    var prevBg = GUI.backgroundColor;
                    if (_sessionDirty)
                        GUI.backgroundColor = new Color(0.45f, 0.9f, 0.45f);

                    var saveLabel = _sessionDirty ? "● Save" : "Save";
                    if (GUILayout.Button(saveLabel, EditorStyles.toolbarButton, GUILayout.Width(72f)))
                        SaveAsset();

                    GUI.backgroundColor = prevBg;
                }
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Brush", EditorStyles.miniLabel, GUILayout.Width(40f));
                EditorGUI.BeginChangeCheck();
                _paintDefinition = (MapCellDefinition)EditorGUILayout.ObjectField(
                    _paintDefinition,
                    typeof(MapCellDefinition),
                    false,
                    GUILayout.MinWidth(160f));
                if (EditorGUI.EndChangeCheck())
                    Repaint();

                GUILayout.FlexibleSpace();
                var brushLabel = _paintDefinition == null
                    ? "для Paint и drop"
                    : string.IsNullOrEmpty(_paintDefinition.DisplayName)
                        ? _paintDefinition.name
                        : _paintDefinition.DisplayName;
                GUILayout.Label(brushLabel, EditorStyles.miniLabel);
            }

            if (_serializedConfig != null && _config != null)
            {
                _serializedConfig.Update();
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(_serializedConfig.FindProperty("homeCoord"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        _serializedConfig.ApplyModifiedProperties();
                        MarkSessionDirty();
                    }

                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_selectedInstanceId)))
                    {
                        if (GUILayout.Button("Home ← select", GUILayout.Width(118f)))
                            SetHomeToSelection();
                    }
                }

                EditorGUI.BeginChangeCheck();
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(
                        _serializedConfig.FindProperty("interactionRadius"),
                        GUILayout.MinWidth(120f));
                    EditorGUILayout.PropertyField(
                        _serializedConfig.FindProperty("visionRadius"),
                        GUILayout.MinWidth(120f));
                }

                if (EditorGUI.EndChangeCheck())
                {
                    _serializedConfig.ApplyModifiedProperties();
                    MarkSessionDirty();
                }
            }

            _layoutFoldout = EditorGUILayout.Foldout(_layoutFoldout, "Превью: шаг / зум", true);
            if (_layoutFoldout)
            {
                EditorGUI.BeginChangeCheck();
                _baseWidth = EditorGUILayout.FloatField("Base Width", _baseWidth);
                _baseHeight = EditorGUILayout.FloatField("Base Height", _baseHeight);
                _spacingPixels = EditorGUILayout.FloatField("Spacing px", _spacingPixels);
                _stepAdjustX = EditorGUILayout.FloatField("Step Adjust X", _stepAdjustX);
                _stepAdjustY = EditorGUILayout.FloatField("Step Adjust Y", _stepAdjustY);
                _zoom = EditorGUILayout.Slider("Zoom", _zoom, 0.25f, 4f);
                if (EditorGUI.EndChangeCheck())
                    Repaint();

                if (GUILayout.Button("Сбросить пан / центрировать", GUILayout.Width(200f)))
                {
                    _pan = Vector2.zero;
                    Repaint();
                }
            }
        }

        private void DrawStatusBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                // Всегда видно: как двигать камеру.
                GUILayout.Label(
                    ToolStatusHint(),
                    EditorStyles.miniLabel,
                    GUILayout.ExpandWidth(false));

                GUILayout.FlexibleSpace();

                if (_config == null)
                {
                    GUILayout.Label("Назначь MapConfig (ObjectField или drag).", EditorStyles.miniLabel);
                    return;
                }

                var hover = _hoverCoord.HasValue
                    ? $"hover {_hoverCoord.Value}" + (_hoverOccupied ? " (занято)" : " (пусто)")
                    : "hover —";
                var sel = string.IsNullOrEmpty(_selectedInstanceId)
                    ? "select —"
                    : $"select {_selectedInstanceId}";
                var hint = string.IsNullOrEmpty(_statusHint) ? string.Empty : $"   |   {_statusHint}";
                GUILayout.Label(
                    $"{hover}   |   {sel}   |   entries={_coordToEntry.Count}{hint}",
                    EditorStyles.miniLabel);
            }
        }

        private void DrawPreview(Rect rect)
        {
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.17f, 1f));
                Handles.DrawSolidRectangleWithOutline(
                    rect,
                    Color.clear,
                    new Color(0.05f, 0.05f, 0.05f, 1f));
            }

            var halfX = IsoMapLayout.HalfStep(_baseWidth, _spacingPixels, _stepAdjustX);
            var halfY = IsoMapLayout.HalfStep(_baseHeight, _spacingPixels, _stepAdjustY);
            var viewCenter = rect.center;

            if (_config == null || _serializedConfig == null)
            {
                GUI.Label(rect, "Нет карты", EditorStyles.centeredGreyMiniLabel);
                HandlePreviewDragDrop(rect, halfX, halfY, viewCenter);
                return;
            }

            RebuildCoordIndex();
            var mutated = HandlePreviewInput(rect, halfX, halfY, viewCenter);
            mutated |= HandlePreviewDragDrop(rect, halfX, halfY, viewCenter);
            if (mutated)
            {
                RebuildCoordIndex();
                if (_hoverCoord.HasValue)
                    _hoverOccupied = _coordToEntry.ContainsKey(_hoverCoord.Value);
            }
            CollectVisibleCells(rect, viewCenter, halfX, halfY);

            if (Event.current.type == EventType.Repaint)
            {
                // BeginGroup клипает и спрайты, и обводки. RotateAroundPivot / Handles clip ломают.
                GUI.BeginGroup(rect);
                var clip = new Rect(0f, 0f, rect.width, rect.height);
                for (var i = 0; i < _drawBuffer.Count; i++)
                    DrawCellSprite(_drawBuffer[i], rect, viewCenter);

                if (_tool == MapEditTool.Paint
                    && _paintDefinition != null
                    && _hoverCoord.HasValue
                    && !_hoverOccupied)
                {
                    var ghostContent = IsoMapLayout.CoordToAnchored(_hoverCoord.Value, halfX, halfY);
                    var prevColor = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, 0.4f);
                    DrawCellSprite(
                        new PreviewCell
                        {
                            Coord = _hoverCoord.Value,
                            Sprite = _paintDefinition.Sprite,
                            ContentPos = ghostContent
                        },
                        rect,
                        viewCenter);
                    GUI.color = prevColor;
                }

                if (!string.IsNullOrEmpty(_selectedInstanceId))
                {
                    for (var i = 0; i < _drawBuffer.Count; i++)
                    {
                        if (_drawBuffer[i].InstanceId != _selectedInstanceId)
                            continue;
                        DrawDiamondOutlineLocal(
                            ContentToView(_drawBuffer[i].ContentPos, viewCenter) - rect.min,
                            new Color(1f, 0.85f, 0.2f, 1f),
                            2f,
                            clip);
                        break;
                    }
                }

                // Hover: цвет зависит от инструмента (add / replace / erase / move dest).
                if (_hoverCoord.HasValue)
                {
                    var hoverContent = IsoMapLayout.CoordToAnchored(_hoverCoord.Value, halfX, halfY);
                    HoverStyle(out var hoverColor, out var hoverThickness);
                    DrawDiamondOutlineLocal(
                        ContentToView(hoverContent, viewCenter) - rect.min,
                        hoverColor,
                        hoverThickness,
                        clip);
                }

                GUI.EndGroup();
            }
        }

        private bool HandlePreviewInput(Rect rect, float halfX, float halfY, Vector2 viewCenter)
        {
            var e = Event.current;
            if (!rect.Contains(e.mousePosition) && e.type != EventType.MouseUp && e.type != EventType.MouseDrag)
                return false;

            if (e.type == EventType.MouseMove
                || e.type == EventType.MouseDrag
                || e.type == EventType.Repaint
                || e.type == EventType.DragUpdated)
            {
                if (rect.Contains(e.mousePosition))
                {
                    var content = ViewToContent(e.mousePosition, viewCenter);
                    _hoverCoord = IsoMapLayout.AnchoredToCoord(content, halfX, halfY);
                    _hoverOccupied = _coordToEntry.ContainsKey(_hoverCoord.Value);
                }
                else if (!_isPanning && !_strokeActive)
                {
                    _hoverCoord = null;
                    _hoverOccupied = false;
                }
            }

            if (e.type == EventType.ScrollWheel && rect.Contains(e.mousePosition))
            {
                // Зум к курсору: content-точка под мышью остаётся на месте.
                var contentUnderMouse = ViewToContent(e.mousePosition, viewCenter);
                var factor = e.delta.y > 0f ? 0.9f : 1.1f;
                _zoom = Mathf.Clamp(_zoom * factor, 0.25f, 4f);
                var newView = ContentToView(contentUnderMouse, viewCenter);
                _pan += (Vector2)e.mousePosition - newView;
                e.Use();
                Repaint();
                return false;
            }

            // Средняя кнопка / Alt+ЛКМ — пан.
            var panButton = e.button == 2 || (e.button == 0 && e.alt);
            if (e.type == EventType.MouseDown && panButton && rect.Contains(e.mousePosition))
            {
                _isPanning = true;
                _strokeActive = false;
                _panStartMouse = e.mousePosition;
                _panStartOffset = _pan;
                e.Use();
                return false;
            }

            if (e.type == EventType.MouseDrag && _isPanning)
            {
                _pan = _panStartOffset + ((Vector2)e.mousePosition - _panStartMouse);
                e.Use();
                Repaint();
                return false;
            }

            if (e.type == EventType.MouseUp && _isPanning)
            {
                _isPanning = false;
                e.Use();
                return false;
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                _strokeActive = false;
                _lastStrokeCoord = null;
            }

            var toolClick = e.button == 0 && !e.alt && !_isPanning;
            var isDown = e.type == EventType.MouseDown && toolClick && rect.Contains(e.mousePosition);
            var isDrag = e.type == EventType.MouseDrag && toolClick && _strokeActive;
            if (!isDown && !isDrag)
                return false;

            var clickContent = ViewToContent(e.mousePosition, viewCenter);
            var coord = IsoMapLayout.AnchoredToCoord(clickContent, halfX, halfY);
            if (isDown)
            {
                _strokeActive = AllowsStroke(_tool);
                _lastStrokeCoord = null;
                GUIUtility.keyboardControl = 0;
            }

            if (_lastStrokeCoord.HasValue && _lastStrokeCoord.Value == coord)
            {
                e.Use();
                return false;
            }

            if (AllowsStroke(_tool))
                _lastStrokeCoord = coord;

            var mutated = ApplyToolAt(coord, isDrag);
            e.Use();
            Repaint();
            return mutated;
        }

        private void CollectVisibleCells(
            Rect rect,
            Vector2 viewCenter,
            float halfX,
            float halfY)
        {
            _drawBuffer.Clear();
            var entries = _serializedConfig.FindProperty("entries");
            if (entries == null || !entries.isArray)
                return;

            // Расширяем view в content-space по углам + запас на размер спрайта.
            var pad = Mathf.Max(_baseWidth, _baseHeight) * _zoom;
            var c0 = ViewToContent(new Vector2(rect.xMin - pad, rect.yMin - pad), viewCenter);
            var c1 = ViewToContent(new Vector2(rect.xMax + pad, rect.yMin - pad), viewCenter);
            var c2 = ViewToContent(new Vector2(rect.xMin - pad, rect.yMax + pad), viewCenter);
            var c3 = ViewToContent(new Vector2(rect.xMax + pad, rect.yMax + pad), viewCenter);
            var minX = Mathf.Min(c0.x, c1.x, c2.x, c3.x);
            var maxX = Mathf.Max(c0.x, c1.x, c2.x, c3.x);
            var minY = Mathf.Min(c0.y, c1.y, c2.y, c3.y);
            var maxY = Mathf.Max(c0.y, c1.y, c2.y, c3.y);

            for (var i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                var coord = entry.FindPropertyRelative("Coord").vector2IntValue;
                var content = IsoMapLayout.CoordToAnchored(coord, halfX, halfY);
                if (content.x < minX || content.x > maxX || content.y < minY || content.y > maxY)
                    continue;

                var cell = entry.FindPropertyRelative("Cell").objectReferenceValue as MapCellDefinition;
                _drawBuffer.Add(new PreviewCell
                {
                    Coord = coord,
                    InstanceId = entry.FindPropertyRelative("InstanceId").stringValue ?? string.Empty,
                    Sprite = cell != null ? cell.Sprite : null,
                    ContentPos = content
                });
            }

            _drawBuffer.Sort((a, b) => IsoMapLayout.CompareDepthBackToFront(a.Coord, b.Coord));
        }

        private void DrawCellSprite(PreviewCell cell, Rect previewRect, Vector2 viewCenter)
        {
            // Как MapCellView: anchored = центр footprint; pivot спрайта на низе footprint.
            var pivotContent = cell.ContentPos + new Vector2(0f, -_baseHeight * 0.5f);
            var localPivot = ContentToView(pivotContent, viewCenter) - previewRect.min;

            if (cell.Sprite == null || cell.Sprite.texture == null)
            {
                var s = 8f * _zoom;
                EditorGUI.DrawRect(
                    new Rect(localPivot.x - s * 0.5f, localPivot.y - s * 0.5f, s, s),
                    new Color(0.45f, 0.45f, 0.5f, 0.9f));
                return;
            }

            var sprite = cell.Sprite;
            var tr = sprite.textureRect;
            var nativeW = Mathf.Max(sprite.rect.width, 0.0001f);
            var nativeH = Mathf.Max(sprite.rect.height, 0.0001f);
            // 1:1 px спрайта × zoom (= MapView mapScale). Без fit под baseWidth —
            // иначе дробный upsample ломает стыки pixel-art.
            var w = nativeW * _zoom;
            var h = nativeH * _zoom;
            var pivot = sprite.pivot;
            var pivotNorm = new Vector2(pivot.x / nativeW, pivot.y / nativeH);
            // GUI Y вниз: pivot.y от низа спрайта → от верха GUI rect.
            var drawRect = new Rect(
                Mathf.Round(localPivot.x - pivotNorm.x * w),
                Mathf.Round(localPivot.y - (1f - pivotNorm.y) * h),
                Mathf.Round(w),
                Mathf.Round(h));

            var texCoords = new Rect(
                tr.x / sprite.texture.width,
                tr.y / sprite.texture.height,
                tr.width / sprite.texture.width,
                tr.height / sprite.texture.height);

            var prevFilter = sprite.texture.filterMode;
            sprite.texture.filterMode = FilterMode.Point;
            GUI.DrawTextureWithTexCoords(drawRect, sprite.texture, texCoords, true);
            sprite.texture.filterMode = prevFilter;
        }

        /// <summary>Ромб в local-координатах группы превью. Без GUI.matrix — иначе clip не держит линии.</summary>
        private void DrawDiamondOutlineLocal(
            Vector2 localCenter,
            Color color,
            float thickness,
            Rect clip)
        {
            var hw = _baseWidth * 0.5f * _zoom;
            var hh = _baseHeight * 0.5f * _zoom;
            var top = localCenter + new Vector2(0f, -hh);
            var right = localCenter + new Vector2(hw, 0f);
            var bottom = localCenter + new Vector2(0f, hh);
            var left = localCenter + new Vector2(-hw, 0f);
            DrawGuiLine(top, right, color, thickness, clip);
            DrawGuiLine(right, bottom, color, thickness, clip);
            DrawGuiLine(bottom, left, color, thickness, clip);
            DrawGuiLine(left, top, color, thickness, clip);
        }

        private static void DrawGuiLine(
            Vector2 from,
            Vector2 to,
            Color color,
            float thickness,
            Rect clip)
        {
            if (!ClipLineToRect(ref from, ref to, clip))
                return;

            var length = Vector2.Distance(from, to);
            if (length < 0.01f)
                return;

            var steps = Mathf.Max(1, Mathf.CeilToInt(length));
            var half = Mathf.Max(1f, thickness) * 0.5f;
            var size = half * 2f;
            var prevColor = GUI.color;
            GUI.color = color;
            for (var i = 0; i <= steps; i++)
            {
                var p = Vector2.Lerp(from, to, i / (float)steps);
                GUI.DrawTexture(
                    new Rect(p.x - half, p.y - half, size, size),
                    Texture2D.whiteTexture);
            }

            GUI.color = prevColor;
        }

        /// <summary>Liang–Barsky: отрезок внутри clip, иначе линии вылезают на тулбар.</summary>
        private static bool ClipLineToRect(ref Vector2 from, ref Vector2 to, Rect clip)
        {
            var dx = to.x - from.x;
            var dy = to.y - from.y;
            var t0 = 0f;
            var t1 = 1f;
            if (!ClipEdge(-dx, from.x - clip.xMin, ref t0, ref t1))
                return false;
            if (!ClipEdge(dx, clip.xMax - from.x, ref t0, ref t1))
                return false;
            if (!ClipEdge(-dy, from.y - clip.yMin, ref t0, ref t1))
                return false;
            if (!ClipEdge(dy, clip.yMax - from.y, ref t0, ref t1))
                return false;

            if (t1 < 1f)
                to = new Vector2(from.x + t1 * dx, from.y + t1 * dy);
            if (t0 > 0f)
                from = new Vector2(from.x + t0 * dx, from.y + t0 * dy);
            return true;
        }

        private static bool ClipEdge(float p, float q, ref float t0, ref float t1)
        {
            if (Mathf.Abs(p) < 0.00001f)
                return q >= 0f;

            var r = q / p;
            if (p < 0f)
            {
                if (r > t1)
                    return false;
                if (r > t0)
                    t0 = r;
            }
            else
            {
                if (r < t0)
                    return false;
                if (r < t1)
                    t1 = r;
            }

            return true;
        }

        private bool HandlePreviewDragDrop(Rect rect, float halfX, float halfY, Vector2 viewCenter)
        {
            var e = Event.current;
            if (!rect.Contains(e.mousePosition))
                return false;

            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform)
                return false;

            var definition = FindDragged<MapCellDefinition>();
            var config = FindDragged<MapConfig>();

            if (definition != null && _config != null && _serializedConfig != null)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                var content = ViewToContent(e.mousePosition, viewCenter);
                _hoverCoord = IsoMapLayout.AnchoredToCoord(content, halfX, halfY);
                _hoverOccupied = _coordToEntry.ContainsKey(_hoverCoord.Value);
                if (e.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    _paintDefinition = definition;
                    var mutated = PaintAt(_hoverCoord.Value);
                    e.Use();
                    Repaint();
                    return mutated;
                }

                e.Use();
                Repaint();
                return false;
            }

            if (config == null)
                return false;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                TryBindConfig(config);
            }

            e.Use();
            return false;
        }

        private void HandleToolHotkeys()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown)
                return;
            if (e.alt || e.control || e.command || EditorGUIUtility.editingTextField)
                return;

            switch (e.keyCode)
            {
                case KeyCode.Alpha1:
                case KeyCode.Keypad1:
                    _tool = MapEditTool.Select;
                    break;
                case KeyCode.Alpha2:
                case KeyCode.Keypad2:
                    _tool = MapEditTool.Paint;
                    break;
                case KeyCode.Alpha3:
                case KeyCode.Keypad3:
                    _tool = MapEditTool.Erase;
                    break;
                case KeyCode.Alpha4:
                case KeyCode.Keypad4:
                    _tool = MapEditTool.Move;
                    break;
                case KeyCode.Escape:
                    _selectedInstanceId = string.Empty;
                    _statusHint = string.Empty;
                    break;
                default:
                    return;
            }

            e.Use();
            Repaint();
        }

        private void DrawToolToggle(MapEditTool tool, string label, string tooltip)
        {
            var on = _tool == tool;
            var prev = GUI.backgroundColor;
            if (on)
                GUI.backgroundColor = new Color(0.55f, 0.8f, 1f);
            if (GUILayout.Toggle(on, new GUIContent(label, tooltip), EditorStyles.toolbarButton, GUILayout.Width(52f)))
                _tool = tool;
            GUI.backgroundColor = prev;
        }

        private string ToolStatusHint()
        {
            return _tool switch
            {
                MapEditTool.Paint => "Пан: СКМ / Alt+ЛКМ   ·   Зум: колесо   ·   Paint: ЛКМ / drag",
                MapEditTool.Erase => "Пан: СКМ / Alt+ЛКМ   ·   Зум: колесо   ·   Erase: ЛКМ / drag",
                MapEditTool.Move => "Пан: СКМ / Alt+ЛКМ   ·   Зум: колесо   ·   Move: pick → пустой coord",
                _ => "Пан: СКМ / Alt+ЛКМ   ·   Зум: колесо   ·   Select: ЛКМ"
            };
        }

        private static bool AllowsStroke(MapEditTool tool)
            => tool == MapEditTool.Paint || tool == MapEditTool.Erase;

        private bool ApplyToolAt(Vector2Int coord, bool isDrag)
        {
            switch (_tool)
            {
                case MapEditTool.Select:
                    if (isDrag)
                        return false;
                    SelectAt(coord);
                    return false;
                case MapEditTool.Paint:
                    return PaintAt(coord);
                case MapEditTool.Erase:
                    return EraseAt(coord);
                case MapEditTool.Move:
                    if (isDrag)
                        return false;
                    return MoveClick(coord);
                default:
                    return false;
            }
        }

        private void SelectAt(Vector2Int coord)
        {
            _selectedInstanceId = _coordToEntry.TryGetValue(coord, out var entryIndex)
                ? InstanceIdAt(entryIndex)
                : string.Empty;
            _statusHint = string.Empty;
        }

        private bool PaintAt(Vector2Int coord)
        {
            if (_paintDefinition == null)
            {
                _statusHint = "Paint: назначь Brush (MapCellDefinition).";
                return false;
            }

            if (!MapConfigEntriesEditor.TryPaintCell(
                    _serializedConfig, coord, _paintDefinition, out var instanceId))
            {
                if (!string.IsNullOrEmpty(instanceId))
                    _selectedInstanceId = instanceId;
                return false;
            }

            _selectedInstanceId = instanceId;
            _statusHint = _coordToEntry.ContainsKey(coord) ? $"replace {coord}" : $"add {coord}";
            MarkSessionDirty();
            return true;
        }

        private bool EraseAt(Vector2Int coord)
        {
            var home = _serializedConfig.FindProperty("homeCoord")?.vector2IntValue;
            if (!MapConfigEntriesEditor.TryEraseCell(_serializedConfig, coord, out var removedId))
                return false;

            if (_selectedInstanceId == removedId)
                _selectedInstanceId = string.Empty;

            _statusHint = home.HasValue && home.Value == coord
                ? $"erase {coord} (это был HomeCoord — назначь Home ← select)"
                : $"erase {coord}";
            MarkSessionDirty();
            return true;
        }

        private bool MoveClick(Vector2Int coord)
        {
            if (_coordToEntry.TryGetValue(coord, out var occupiedIndex))
            {
                _selectedInstanceId = InstanceIdAt(occupiedIndex);
                _statusHint = string.IsNullOrEmpty(_selectedInstanceId)
                    ? "Move: выбери клетку"
                    : "Move: клик по пустому coord";
                return false;
            }

            if (string.IsNullOrEmpty(_selectedInstanceId))
            {
                _statusHint = "Move: сначала выбери клетку.";
                return false;
            }

            if (!MapConfigEntriesEditor.TryMoveCell(
                    _serializedConfig, _selectedInstanceId, coord, out var error))
            {
                if (!string.IsNullOrEmpty(error))
                    _statusHint = error;
                return false;
            }

            _statusHint = $"move → {coord}";
            MarkSessionDirty();
            return true;
        }

        private void SetHomeToSelection()
        {
            if (!MapConfigEntriesEditor.TryGetCoordByInstanceId(
                    _serializedConfig, _selectedInstanceId, out var coord))
            {
                _statusHint = "Home: нет выбранной клетки.";
                return;
            }

            if (!MapConfigEntriesEditor.TrySetHomeCoord(_serializedConfig, coord))
                return;

            _statusHint = $"homeCoord = {coord}";
            if (_coordToEntry.TryGetValue(coord, out var homeIndex))
            {
                var cell = _serializedConfig.FindProperty("entries")
                    ?.GetArrayElementAtIndex(homeIndex)
                    .FindPropertyRelative("Cell").objectReferenceValue as MapCellDefinition;
                if (cell?.Behavior is not HomeMapCellBehavior)
                    _statusHint += " (нужен HomeMapCellBehavior)";
            }

            MarkSessionDirty();
            Repaint();
        }

        private void HoverStyle(out Color color, out float thickness)
        {
            thickness = _hoverOccupied ? 2f : 1.5f;
            var hoverIsSelected = false;
            if (_hoverOccupied
                && _hoverCoord.HasValue
                && !string.IsNullOrEmpty(_selectedInstanceId)
                && _coordToEntry.TryGetValue(_hoverCoord.Value, out var hoverIndex))
            {
                hoverIsSelected = InstanceIdAt(hoverIndex) == _selectedInstanceId;
            }

            switch (_tool)
            {
                case MapEditTool.Paint:
                    color = _hoverOccupied
                        ? new Color(1f, 0.7f, 0.25f, 0.95f)
                        : new Color(0.4f, 1f, 0.55f, 0.9f);
                    return;
                case MapEditTool.Erase:
                    color = _hoverOccupied
                        ? new Color(1f, 0.35f, 0.3f, 0.95f)
                        : new Color(0.4f, 0.75f, 1f, 0.7f);
                    return;
                case MapEditTool.Move:
                    if (!_hoverOccupied)
                    {
                        color = string.IsNullOrEmpty(_selectedInstanceId)
                            ? new Color(0.4f, 0.75f, 1f, 0.85f)
                            : new Color(0.4f, 1f, 0.55f, 0.9f);
                        return;
                    }

                    color = hoverIsSelected
                        ? new Color(1f, 0.85f, 0.2f, 1f)
                        : string.IsNullOrEmpty(_selectedInstanceId)
                            ? new Color(0.55f, 0.9f, 1f, 0.95f)
                            : new Color(1f, 0.35f, 0.3f, 0.95f);
                    return;
                default:
                    color = _hoverOccupied
                        ? new Color(0.55f, 0.9f, 1f, 0.95f)
                        : new Color(0.4f, 0.75f, 1f, 0.85f);
                    return;
            }
        }

        private string InstanceIdAt(int entryIndex)
        {
            var entries = _serializedConfig.FindProperty("entries");
            if (entries == null || entryIndex < 0 || entryIndex >= entries.arraySize)
                return string.Empty;
            return entries.GetArrayElementAtIndex(entryIndex)
                .FindPropertyRelative("InstanceId").stringValue ?? string.Empty;
        }

        private static T FindDragged<T>() where T : UnityEngine.Object
        {
            var refs = DragAndDrop.objectReferences;
            if (refs == null)
                return null;
            for (var i = 0; i < refs.Length; i++)
            {
                if (refs[i] is T typed)
                    return typed;
            }

            return null;
        }

        private Vector2 ContentToView(Vector2 content, Vector2 viewCenter)
        {
            // UI Y вверх → GUI Y вниз. Round — стыки pixel-art без субпиксельных щелей.
            return new Vector2(
                Mathf.Round(viewCenter.x + content.x * _zoom + _pan.x),
                Mathf.Round(viewCenter.y - content.y * _zoom + _pan.y));
        }

        private Vector2 ViewToContent(Vector2 viewPos, Vector2 viewCenter)
        {
            return new Vector2(
                (viewPos.x - viewCenter.x - _pan.x) / Mathf.Max(_zoom, 0.0001f),
                -((viewPos.y - viewCenter.y - _pan.y) / Mathf.Max(_zoom, 0.0001f)));
        }

        private void RebuildCoordIndex()
        {
            _coordToEntry.Clear();
            if (_serializedConfig == null)
                return;

            _serializedConfig.Update();
            var entries = _serializedConfig.FindProperty("entries");
            if (entries == null || !entries.isArray)
                return;

            for (var i = 0; i < entries.arraySize; i++)
            {
                var coord = entries.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("Coord").vector2IntValue;
                _coordToEntry[coord] = i;
            }
        }

        private void TryBindConfig(MapConfig next)
        {
            if (ReferenceEquals(next, _config))
                return;

            if (!TryLeaveCurrentMap())
                return;

            BindConfig(next);
        }

        private bool TryLeaveCurrentMap()
        {
            if (!_sessionDirty || _config == null)
                return true;

            var result = EditorUtility.DisplayDialogComplex(
                "Map Editor",
                $"Сохранить изменения в '{_config.name}'?",
                "Да",
                "Отмена",
                "Нет");

            switch (result)
            {
                case 0: // Да
                    SaveAsset();
                    return true;
                case 1: // Отмена
                    return false;
                default: // Нет
                    ReloadFromDisk();
                    return true;
            }
        }

        private void BindConfig(MapConfig config)
        {
            _config = config;
            _selectedInstanceId = string.Empty;
            _hoverCoord = null;
            _pan = Vector2.zero;
            _strokeActive = false;
            _lastStrokeCoord = null;
            _statusHint = string.Empty;

            if (_config == null)
            {
                _serializedConfig = null;
                ClearSessionDirty();
                return;
            }

            _serializedConfig = new SerializedObject(_config);
            var filled = MapConfigInstanceIdUtility.FillEmptyInstanceIds(_serializedConfig);
            _serializedConfig.Update();

            if (filled > 0)
            {
                EditorUtility.SetDirty(_config);
                MarkSessionDirty();
                Debug.Log(
                    $"[MapEditor] Заполнено пустых InstanceId: {filled} (Save, чтобы записать ассет).");
            }
            else
            {
                ClearSessionDirty();
            }

            saveChangesMessage =
                $"Сохранить изменения MapConfig «{_config.name}»?";
        }

        private void SaveAsset()
        {
            if (_config == null || _serializedConfig == null)
                return;

            _serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();
            ClearSessionDirty();
            Debug.Log($"[MapEditor] Сохранено: {AssetDatabase.GetAssetPath(_config)}");
        }

        private void ReloadFromDisk()
        {
            if (_config == null)
            {
                ClearSessionDirty();
                return;
            }

            var path = AssetDatabase.GetAssetPath(_config);
            EditorUtility.ClearDirty(_config);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            _config = AssetDatabase.LoadAssetAtPath<MapConfig>(path);
            _serializedConfig = _config != null ? new SerializedObject(_config) : null;
            _selectedInstanceId = string.Empty;
            _strokeActive = false;
            _lastStrokeCoord = null;
            _statusHint = string.Empty;
            ClearSessionDirty();
        }

        private void MarkSessionDirty()
        {
            _sessionDirty = true;
            SyncUnsavedFlag();
        }

        private void ClearSessionDirty()
        {
            _sessionDirty = false;
            SyncUnsavedFlag();
        }

        private void SyncUnsavedFlag()
        {
            hasUnsavedChanges = _sessionDirty;
            if (_config != null)
                saveChangesMessage = $"Сохранить изменения MapConfig «{_config.name}»?";
        }
    }
}
