using IdleTower.Data.Definitions;
using IdleTower.Map.Behaviors;
using UnityEditor;
using UnityEngine;

namespace IdleTower.Editor
{
    /// <summary>
    /// Inspector MapConfig: выбранная в Map Editor entry, иначе настройки карты.
    /// Полный список entries — свёрнутый foldout «All».
    /// </summary>
    [CustomEditor(typeof(MapConfig))]
    public sealed class MapConfigEditor : UnityEditor.Editor
    {
        private static bool _showAllEntries;

        private void OnEnable()
        {
            MapEditorSelection.Changed += Repaint;
        }

        private void OnDisable()
        {
            MapEditorSelection.Changed -= Repaint;
        }

        public override void OnInspectorGUI()
        {
            var config = (MapConfig)target;
            var so = SessionObject(config);
            if (so == null)
                return;

            so.Update();
            EditorGUI.BeginChangeCheck();

            if (TryGetSelectedEntry(config, so, out var entry))
                DrawSelectedEntry(entry);
            else
                DrawMapSettings(so);

            EditorGUILayout.Space(6f);
            _showAllEntries = EditorGUILayout.Foldout(_showAllEntries, "All", true);
            if (_showAllEntries)
                EditorGUILayout.PropertyField(so.FindProperty("entries"), true);

            if (EditorGUI.EndChangeCheck() && so.ApplyModifiedProperties())
                NotifyWindow(config);
        }

        private SerializedObject SessionObject(MapConfig config)
        {
            var window = MapEditorSelection.Window;
            if (window != null
                && ReferenceEquals(window.SessionConfig, config)
                && window.SessionSerializedObject != null)
                return window.SessionSerializedObject;

            return serializedObject;
        }

        private static bool TryGetSelectedEntry(
            MapConfig config,
            SerializedObject so,
            out SerializedProperty entry)
        {
            entry = null;
            if (!ReferenceEquals(MapEditorSelection.Config, config))
                return false;

            var instanceId = MapEditorSelection.InstanceId;
            if (string.IsNullOrEmpty(instanceId))
                return false;

            var entries = so.FindProperty("entries");
            if (entries == null || !entries.isArray)
                return false;

            for (var i = 0; i < entries.arraySize; i++)
            {
                var candidate = entries.GetArrayElementAtIndex(i);
                var id = candidate.FindPropertyRelative("InstanceId");
                if (id != null && id.stringValue == instanceId)
                {
                    entry = candidate;
                    return true;
                }
            }

            EditorGUILayout.HelpBox(
                "Выбранная в Map Editor клетка не найдена в этом MapConfig.",
                MessageType.Warning);
            return false;
        }

        private static void DrawSelectedEntry(SerializedProperty entry)
        {
            EditorGUILayout.LabelField("Клетка карты", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("InstanceId"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("Coord"));
            }

            EditorGUILayout.LabelField(
                "Координату меняй инструментом Move в Map Editor.",
                EditorStyles.miniLabel);

            var cellProp = entry.FindPropertyRelative("Cell");
            EditorGUILayout.PropertyField(cellProp);

            if (!IsRaid(cellProp))
                return;

            var raid = entry.FindPropertyRelative("Site")?.FindPropertyRelative("Raid");
            if (raid != null)
                EditorGUILayout.PropertyField(raid, true);
        }

        private static void DrawMapSettings(SerializedObject so)
        {
            EditorGUILayout.LabelField("Карта", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("homeCoord"));
            EditorGUILayout.PropertyField(so.FindProperty("interactionRadius"));
            EditorGUILayout.PropertyField(so.FindProperty("visionRadius"));
            EditorGUILayout.PropertyField(so.FindProperty("neighborMode"));
        }

        private static bool IsRaid(SerializedProperty cellProp)
        {
            var cell = cellProp?.objectReferenceValue as MapCellDefinition;
            return cell != null && cell.Behavior is RaidMapCellBehavior;
        }

        private static void NotifyWindow(MapConfig config)
        {
            var window = MapEditorSelection.Window;
            if (window != null && ReferenceEquals(window.SessionConfig, config))
                window.NotifyInspectorEdited();
        }
    }
}
