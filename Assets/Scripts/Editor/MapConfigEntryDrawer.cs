using IdleTower.Data.Definitions;
using IdleTower.Map.Behaviors;
using UnityEditor;
using UnityEngine;

namespace IdleTower.Editor
{
    /// <summary>
    /// Site.Raid только если Cell.Behavior = Raid; иначе Site в инспекторе скрыт.
    /// </summary>
    [CustomPropertyDrawer(typeof(MapConfigEntry))]
    public sealed class MapConfigEntryDrawer : PropertyDrawer
    {
        private const float Pad = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            var h = EditorGUIUtility.singleLineHeight + Pad; // foldout
            h += Line(property.FindPropertyRelative("Coord"));
            h += Line(property.FindPropertyRelative("Cell"));

            if (IsRaidEntry(property))
            {
                var site = property.FindPropertyRelative("Site");
                var raid = site?.FindPropertyRelative("Raid");
                if (raid != null)
                    h += EditorGUI.GetPropertyHeight(raid, true) + Pad;
            }

            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var foldRect = new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldRect, property.isExpanded, label, true);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;
            var y = foldRect.yMax + Pad;

            y = DrawProp(position, y, property.FindPropertyRelative("Coord"));
            y = DrawProp(position, y, property.FindPropertyRelative("Cell"));

            if (IsRaidEntry(property))
            {
                var raid = property.FindPropertyRelative("Site")?.FindPropertyRelative("Raid");
                if (raid != null)
                    DrawProp(position, y, raid, includeChildren: true);
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        private static float DrawProp(
            Rect area,
            float y,
            SerializedProperty prop,
            bool includeChildren = true)
        {
            if (prop == null)
                return y;

            var h = EditorGUI.GetPropertyHeight(prop, includeChildren);
            var rect = new Rect(area.x, y, area.width, h);
            EditorGUI.PropertyField(rect, prop, includeChildren);
            return y + h + Pad;
        }

        private static float Line(SerializedProperty prop)
        {
            if (prop == null)
                return 0f;
            return EditorGUI.GetPropertyHeight(prop, true) + Pad;
        }

        private static bool IsRaidEntry(SerializedProperty entry)
        {
            var cellProp = entry.FindPropertyRelative("Cell");
            var cell = cellProp?.objectReferenceValue as MapCellDefinition;
            return cell != null && cell.Behavior is RaidMapCellBehavior;
        }
    }
}
