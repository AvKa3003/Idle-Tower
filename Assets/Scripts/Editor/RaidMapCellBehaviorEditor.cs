using IdleTower.Data.Definitions;
using IdleTower.Map;
using IdleTower.Map.Behaviors;
using UnityEditor;
using UnityEngine;

namespace IdleTower.Editor
{
    [CustomEditor(typeof(RaidMapCellBehavior))]
    public sealed class RaidMapCellBehaviorEditor : UnityEditor.Editor
    {
        private SerializedProperty _hasFunctionalClick;
        private SerializedProperty _preCapture;
        private SerializedProperty _maxCompletedRaids;
        private SerializedProperty _postCaptureMode;
        private SerializedProperty _farmConfig;
        private SerializedProperty _passiveInterval;
        private SerializedProperty _passiveRewards;
        private SerializedProperty _capturedSprite;

        private void OnEnable()
        {
            _hasFunctionalClick = serializedObject.FindProperty("hasFunctionalClick");
            _preCapture = serializedObject.FindProperty("preCapture");
            _maxCompletedRaids = serializedObject.FindProperty("maxCompletedRaids");
            _postCaptureMode = serializedObject.FindProperty("postCaptureMode");
            _farmConfig = serializedObject.FindProperty("farmConfig");
            _passiveInterval = serializedObject.FindProperty("passiveInterval");
            _passiveRewards = serializedObject.FindProperty("passiveRewards");
            _capturedSprite = serializedObject.FindProperty("capturedSprite");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_hasFunctionalClick, new GUIContent("Has Functional Click"));
            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_preCapture, new GUIContent("Pre Capture"), true);
            EditorGUILayout.PropertyField(_maxCompletedRaids, new GUIContent("Max Completed Raids"));
            EditorGUILayout.PropertyField(_postCaptureMode, new GUIContent("Post Capture Mode"));

            var mode = (PostCaptureMode)_postCaptureMode.enumValueIndex;
            EditorGUILayout.Space(6);
            switch (mode)
            {
                case PostCaptureMode.Dead:
                    EditorGUILayout.HelpBox(
                        "Dead: после захвата рейдов нет (этап C). Спрайт Captured — опционально.",
                        MessageType.Info);
                    break;

                case PostCaptureMode.RaidFarm:
                    EditorGUILayout.PropertyField(_farmConfig, new GUIContent("Farm Config"), true);
                    EditorGUILayout.HelpBox("RaidFarm геймплей — этап E.", MessageType.Warning);
                    break;

                case PostCaptureMode.Passive:
                    EditorGUILayout.PropertyField(_passiveInterval, new GUIContent("Passive Interval"), true);
                    EditorGUILayout.PropertyField(_passiveRewards, new GUIContent("Passive Rewards"), true);
                    EditorGUILayout.HelpBox("Passive геймплей — этап F.", MessageType.Warning);
                    break;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(_capturedSprite, new GUIContent("Captured Sprite"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
