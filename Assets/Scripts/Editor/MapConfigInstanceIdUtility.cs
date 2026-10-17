using IdleTower.Data.Definitions;
using UnityEditor;
using UnityEngine;

namespace IdleTower.Editor
{
    /// <summary>Проставляет GUID в MapConfigEntry.InstanceId, если пусто.</summary>
    public static class MapConfigInstanceIdUtility
    {
        [MenuItem("IdleTower/Fix MapConfig Instance Ids")]
        public static void FixAllMapConfigs()
        {
            var guids = AssetDatabase.FindAssets("t:MapConfig");
            var touched = 0;
            var filled = 0;

            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var config = AssetDatabase.LoadAssetAtPath<MapConfig>(path);
                if (config == null)
                    continue;

                var so = new SerializedObject(config);
                var entries = so.FindProperty("entries");
                if (entries == null || !entries.isArray)
                    continue;

                var changed = false;
                for (var e = 0; e < entries.arraySize; e++)
                {
                    var idProp = entries.GetArrayElementAtIndex(e).FindPropertyRelative("InstanceId");
                    if (idProp == null)
                        continue;

                    if (!string.IsNullOrWhiteSpace(idProp.stringValue))
                        continue;

                    idProp.stringValue = System.Guid.NewGuid().ToString("N");
                    filled++;
                    changed = true;
                }

                if (!changed)
                    continue;

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(config);
                touched++;
            }

            if (touched > 0)
                AssetDatabase.SaveAssets();

            Debug.Log(
                $"[MapConfigInstanceIdUtility] Конфигов изменено: {touched}, " +
                $"пустых InstanceId заполнено: {filled}.");
        }
    }
}
