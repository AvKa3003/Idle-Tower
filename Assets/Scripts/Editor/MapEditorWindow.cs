using System.Collections.Generic;
using IdleTower.Data.Definitions;
using IdleTower.Map;
using UnityEditor;
using UnityEngine;

namespace IdleTower.Editor
{
    /// <summary>
    /// Редактор MapConfig: iso-превью, dirty/Save, смена карты.
    /// Paint / Move / Erase — этап 4; Inspector selection — этап 5.
    /// </summary>
    public sealed class MapEditorWindow : EditorWindow
    {
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

                GUILayout.FlexibleSpace();

                var shortcutsContent = new GUIContent(
                    "⇧ Shortcuts",
                    "Превью карты:\n" +
                    "• Пан (камера): СКМ (средняя кнопка) или Alt + ЛКМ\n" +
                    "• Зум: колёсико мыши (к курсору)\n" +
                    "• Select клетки: ЛКМ\n" +
                    "• Сброс пана: кнопка в «Превью: шаг / зум»\n" +
                    "• Назначить карту: ObjectField или drag MapConfig в окно");
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

            if (_serializedConfig != null && _config != null)
            {
                _serializedConfig.Update();
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_serializedConfig.FindProperty("homeCoord"));
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
                    "Пан: СКМ / Alt+ЛКМ   ·   Зум: колесо   ·   Select: ЛКМ",
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
                GUILayout.Label(
                    $"{hover}   |   {sel}   |   entries={_coordToEntry.Count}",
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

            if (_config == null || _serializedConfig == null)
            {
                GUI.Label(rect, "Нет карты", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            var halfX = IsoMapLayout.HalfStep(_baseWidth, _spacingPixels, _stepAdjustX);
            var halfY = IsoMapLayout.HalfStep(_baseHeight, _spacingPixels, _stepAdjustY);
            var viewCenter = rect.center;

            RebuildCoordIndex();
            HandlePreviewInput(rect, halfX, halfY, viewCenter);
            CollectVisibleCells(rect, viewCenter, halfX, halfY);

            if (Event.current.type == EventType.Repaint)
            {
                // BeginGroup клипает и спрайты, и обводки. RotateAroundPivot / Handles clip ломают.
                GUI.BeginGroup(rect);
                var clip = new Rect(0f, 0f, rect.width, rect.height);
                for (var i = 0; i < _drawBuffer.Count; i++)
                    DrawCellSprite(_drawBuffer[i], rect, viewCenter);

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

                // Hover: занятая — подсветка; пустая — призрак под paint.
                if (_hoverCoord.HasValue)
                {
                    var hoverContent = IsoMapLayout.CoordToAnchored(_hoverCoord.Value, halfX, halfY);
                    var hoverColor = _hoverOccupied
                        ? new Color(0.55f, 0.9f, 1f, 0.95f)
                        : new Color(0.4f, 0.75f, 1f, 0.85f);
                    DrawDiamondOutlineLocal(
                        ContentToView(hoverContent, viewCenter) - rect.min,
                        hoverColor,
                        _hoverOccupied ? 2f : 1.5f,
                        clip);
                }

                GUI.EndGroup();
            }

            HandlePreviewDragDrop(rect);
        }

        private void HandlePreviewInput(Rect rect, float halfX, float halfY, Vector2 viewCenter)
        {
            var e = Event.current;
            if (!rect.Contains(e.mousePosition) && e.type != EventType.MouseUp && e.type != EventType.MouseDrag)
                return;

            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag || e.type == EventType.Repaint)
            {
                if (rect.Contains(e.mousePosition))
                {
                    var content = ViewToContent(e.mousePosition, viewCenter);
                    _hoverCoord = IsoMapLayout.AnchoredToCoord(content, halfX, halfY);
                    _hoverOccupied = _coordToEntry.ContainsKey(_hoverCoord.Value);
                }
                else if (!_isPanning)
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
                return;
            }

            // Средняя кнопка / Alt+ЛКМ — пан.
            var panButton = e.button == 2 || (e.button == 0 && e.alt);
            if (e.type == EventType.MouseDown && panButton && rect.Contains(e.mousePosition))
            {
                _isPanning = true;
                _panStartMouse = e.mousePosition;
                _panStartOffset = _pan;
                e.Use();
                return;
            }

            if (e.type == EventType.MouseDrag && _isPanning)
            {
                _pan = _panStartOffset + ((Vector2)e.mousePosition - _panStartMouse);
                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.MouseUp && _isPanning)
            {
                _isPanning = false;
                e.Use();
                return;
            }

            if (e.type == EventType.MouseDown
                && e.button == 0
                && !e.alt
                && rect.Contains(e.mousePosition))
            {
                var content = ViewToContent(e.mousePosition, viewCenter);
                var coord = IsoMapLayout.AnchoredToCoord(content, halfX, halfY);
                if (_coordToEntry.TryGetValue(coord, out var entryIndex))
                {
                    var entries = _serializedConfig.FindProperty("entries");
                    var id = entries.GetArrayElementAtIndex(entryIndex)
                        .FindPropertyRelative("InstanceId").stringValue;
                    _selectedInstanceId = id ?? string.Empty;
                }
                else
                {
                    _selectedInstanceId = string.Empty;
                }

                e.Use();
                Repaint();
            }
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

        private void HandlePreviewDragDrop(Rect rect)
        {
            var e = Event.current;
            if (!rect.Contains(e.mousePosition))
                return;

            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform)
                return;

            var config = DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0
                ? DragAndDrop.objectReferences[0] as MapConfig
                : null;
            if (config == null)
                return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                TryBindConfig(config);
            }

            e.Use();
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
