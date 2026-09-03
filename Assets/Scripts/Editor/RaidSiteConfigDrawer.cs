using IdleTower.Data.Definitions;
using IdleTower.Map;
using UnityEditor;
using UnityEngine;

namespace IdleTower.Editor
{
    /// <summary>
    /// Farm/Passive поля только при выбранном PostCaptureMode.
    /// </summary>
    [CustomPropertyDrawer(typeof(RaidSiteConfig))]
    public sealed class RaidSiteConfigDrawer : PropertyDrawer
    {
        private const float Pad = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            var h = EditorGUIUtility.singleLineHeight + Pad; // foldout
            h += Line(property, "PreCapture");
            h += Line(property, "MaxCompletedRaids");
            h += Line(property, "PostCaptureMode");

            var mode = (PostCaptureMode)property.FindPropertyRelative("PostCaptureMode").enumValueIndex;
            if (mode == PostCaptureMode.RaidFarm)
                h += Line(property, "FarmConfig");
            else if (mode == PostCaptureMode.Passive)
            {
                h += Line(property, "PassiveInterval");
                h += Line(property, "PassiveRewards");
            }

            h += Line(property, "CapturedSprite");
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

            y = Draw(position, y, property, "PreCapture");
            y = Draw(position, y, property, "MaxCompletedRaids");
            y = Draw(position, y, property, "PostCaptureMode");

            var mode = (PostCaptureMode)property.FindPropertyRelative("PostCaptureMode").enumValueIndex;
            if (mode == PostCaptureMode.RaidFarm)
                y = Draw(position, y, property, "FarmConfig");
            else if (mode == PostCaptureMode.Passive)
            {
                y = Draw(position, y, property, "PassiveInterval");
                y = Draw(position, y, property, "PassiveRewards");
            }

            Draw(position, y, property, "CapturedSprite");

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        private static float Draw(Rect area, float y, SerializedProperty root, string name)
        {
            var prop = root.FindPropertyRelative(name);
            if (prop == null)
                return y;

            var h = EditorGUI.GetPropertyHeight(prop, true);
            EditorGUI.PropertyField(new Rect(area.x, y, area.width, h), prop, true);
            return y + h + Pad;
        }

        private static float Line(SerializedProperty root, string name)
        {
            var prop = root.FindPropertyRelative(name);
            return prop == null ? 0f : EditorGUI.GetPropertyHeight(prop, true) + Pad;
        }
    }
}
