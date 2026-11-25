using UnityEditor;
using UnityEngine;

namespace IdleTower.Editor
{
    /// <summary>
    /// Decal клетки: обычное поле спрайта, в Object Picker сразу фильтр папки.
    /// </summary>
    internal static class MapDecalSpriteField
    {
        private const string Folder = "Assets/Textures/Export/MapCellsDecal";
        private const string PickerFilter = "glob:\"Assets/Textures/Export/MapCellsDecal/**\"";

        private static int _pickerId;
        private static string _pickerPath = string.Empty;

        public static void Layout(SerializedProperty property)
        {
            Draw(EditorGUILayout.GetControlRect(), property);
        }

        public static void Draw(Rect position, SerializedProperty property)
        {
            if (property == null)
                return;

            var evt = Event.current;
            if (evt.type == EventType.ExecuteCommand
                && evt.commandName == "ObjectSelectorUpdated"
                && EditorGUIUtility.GetObjectPickerControlID() == _pickerId
                && property.propertyPath == _pickerPath)
            {
                var picked = EditorGUIUtility.GetObjectPickerObject();
                var path = picked != null ? AssetDatabase.GetAssetPath(picked).Replace('\\', '/') : string.Empty;
                if (picked == null || path.StartsWith(Folder + "/"))
                {
                    property.objectReferenceValue = picked;
                    GUI.changed = true;
                }

                evt.Use();
            }

            var label = EditorGUI.BeginProperty(position, new GUIContent(property.displayName, property.tooltip), property);
            position = EditorGUI.PrefixLabel(position, label);
            if (GUI.Button(position, EditorGUIUtility.ObjectContent(property.objectReferenceValue, typeof(Sprite)), EditorStyles.objectField))
            {
                _pickerId = GUIUtility.GetControlID(FocusType.Passive);
                _pickerPath = property.propertyPath;
                EditorGUIUtility.ShowObjectPicker<Sprite>(property.objectReferenceValue, false, PickerFilter, _pickerId);
            }

            EditorGUI.EndProperty();
        }
    }
}
