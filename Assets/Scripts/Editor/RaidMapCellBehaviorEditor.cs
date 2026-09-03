using IdleTower.Map.Behaviors;
using UnityEditor;
using UnityEngine;

namespace IdleTower.Editor
{
    [CustomEditor(typeof(RaidMapCellBehavior))]
    public sealed class RaidMapCellBehaviorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "RaidMapCellBehavior — только тип клетки (клик, захват, reveal).\n" +
                "Баланс рейда задаётся в MapConfig → Entry → Site → Raid\n" +
                "(PreCapture, MaxCompletedRaids, PostCaptureMode, Farm/Passive поля).",
                MessageType.Info);

            DrawDefaultInspector();
        }
    }
}
