using IdleTower.Data.Definitions;
using UnityEditor;
using UnityEngine;

namespace IdleTower.Editor
{
    /// <summary>
    /// Strength в Inspector только при IsUnit.
    /// При снятии галочки значение strength в ассете не трогаем.
    /// </summary>
    [CustomEditor(typeof(ResourceDefinition))]
    public sealed class ResourceDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty _id;
        private SerializedProperty _displayName;
        private SerializedProperty _icon;
        private SerializedProperty _isUnit;
        private SerializedProperty _strength;

        private void OnEnable()
        {
            _id = serializedObject.FindProperty("id");
            _displayName = serializedObject.FindProperty("displayName");
            _icon = serializedObject.FindProperty("icon");
            _isUnit = serializedObject.FindProperty("isUnit");
            _strength = serializedObject.FindProperty("strength");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_id);
            EditorGUILayout.PropertyField(_displayName);
            EditorGUILayout.PropertyField(_icon);
            EditorGUILayout.PropertyField(_isUnit, new GUIContent("Is Unit"));

            if (_isUnit.boolValue)
                EditorGUILayout.PropertyField(_strength, new GUIContent("Strength"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
