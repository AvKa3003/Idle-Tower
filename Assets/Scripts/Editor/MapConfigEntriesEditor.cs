using IdleTower.Data.Definitions;
using UnityEditor;
using UnityEngine;

namespace IdleTower.Editor
{
    /// <summary>
    /// Правки массива MapConfig.entries через SerializedObject.
    /// Увеличение arraySize копирует предыдущий элемент — новые entry всегда
    /// перезаписываются целиком (новый InstanceId, пустой Site).
    /// </summary>
    internal static class MapConfigEntriesEditor
    {
        public static bool TryPaintCell(
            SerializedObject so,
            Vector2Int coord,
            MapCellDefinition definition,
            out string instanceId)
        {
            instanceId = string.Empty;
            if (so == null || definition == null)
                return false;

            so.Update();
            var entries = Entries(so);
            if (entries == null)
                return false;

            var index = FindIndexByCoord(entries, coord);
            if (index >= 0)
            {
                var entry = entries.GetArrayElementAtIndex(index);
                instanceId = entry.FindPropertyRelative("InstanceId").stringValue ?? string.Empty;
                var cellProp = entry.FindPropertyRelative("Cell");
                if (ReferenceEquals(cellProp.objectReferenceValue, definition))
                    return false;

                // Занято: только Cell. Site и InstanceId остаются.
                cellProp.objectReferenceValue = definition;
                so.ApplyModifiedProperties();
                return true;
            }

            instanceId = MapConfigInstanceIdUtility.CreateInstanceId();
            var newIndex = entries.arraySize;
            entries.arraySize = newIndex + 1;
            WriteNewEntry(entries.GetArrayElementAtIndex(newIndex), coord, definition, instanceId);
            so.ApplyModifiedProperties();
            return true;
        }

        public static bool TryEraseCell(
            SerializedObject so,
            Vector2Int coord,
            out string removedInstanceId)
        {
            removedInstanceId = string.Empty;
            if (so == null)
                return false;

            so.Update();
            var entries = Entries(so);
            if (entries == null)
                return false;

            var index = FindIndexByCoord(entries, coord);
            if (index < 0)
                return false;

            removedInstanceId = entries.GetArrayElementAtIndex(index)
                .FindPropertyRelative("InstanceId").stringValue ?? string.Empty;
            entries.DeleteArrayElementAtIndex(index);
            so.ApplyModifiedProperties();
            return true;
        }

        public static bool TryMoveCell(
            SerializedObject so,
            string instanceId,
            Vector2Int to,
            out string error)
        {
            error = string.Empty;
            if (so == null || string.IsNullOrEmpty(instanceId))
            {
                error = "Move: нет выбранной клетки.";
                return false;
            }

            so.Update();
            var entries = Entries(so);
            if (entries == null)
            {
                error = "Move: нет entries.";
                return false;
            }

            var fromIndex = FindIndexByInstanceId(entries, instanceId);
            if (fromIndex < 0)
            {
                error = "Move: выбранная клетка не найдена.";
                return false;
            }

            var fromEntry = entries.GetArrayElementAtIndex(fromIndex);
            var fromCoord = fromEntry.FindPropertyRelative("Coord").vector2IntValue;
            if (fromCoord == to)
                return false;

            var occupied = FindIndexByCoord(entries, to);
            if (occupied >= 0 && occupied != fromIndex)
            {
                error = $"Move: клетка {to} занята.";
                return false;
            }

            fromEntry.FindPropertyRelative("Coord").vector2IntValue = to;

            var home = so.FindProperty("homeCoord");
            if (home != null && home.vector2IntValue == fromCoord)
                home.vector2IntValue = to;

            so.ApplyModifiedProperties();
            return true;
        }

        public static bool TrySetHomeCoord(SerializedObject so, Vector2Int coord)
        {
            if (so == null)
                return false;

            so.Update();
            var home = so.FindProperty("homeCoord");
            if (home == null || home.vector2IntValue == coord)
                return false;

            home.vector2IntValue = coord;
            so.ApplyModifiedProperties();
            return true;
        }

        public static bool TryGetCoordByInstanceId(
            SerializedObject so,
            string instanceId,
            out Vector2Int coord)
        {
            coord = default;
            if (so == null || string.IsNullOrEmpty(instanceId))
                return false;

            so.Update();
            var entries = Entries(so);
            if (entries == null)
                return false;

            var index = FindIndexByInstanceId(entries, instanceId);
            if (index < 0)
                return false;

            coord = entries.GetArrayElementAtIndex(index)
                .FindPropertyRelative("Coord").vector2IntValue;
            return true;
        }

        private static SerializedProperty Entries(SerializedObject so)
        {
            var entries = so.FindProperty("entries");
            return entries != null && entries.isArray ? entries : null;
        }

        private static int FindIndexByCoord(SerializedProperty entries, Vector2Int coord)
        {
            var found = -1;
            for (var i = 0; i < entries.arraySize; i++)
            {
                var c = entries.GetArrayElementAtIndex(i).FindPropertyRelative("Coord");
                if (c != null && c.vector2IntValue == coord)
                    found = i;
            }

            return found;
        }

        private static int FindIndexByInstanceId(SerializedProperty entries, string instanceId)
        {
            for (var i = 0; i < entries.arraySize; i++)
            {
                var id = entries.GetArrayElementAtIndex(i).FindPropertyRelative("InstanceId");
                if (id != null && id.stringValue == instanceId)
                    return i;
            }

            return -1;
        }

        private static void WriteNewEntry(
            SerializedProperty entry,
            Vector2Int coord,
            MapCellDefinition definition,
            string instanceId)
        {
            entry.FindPropertyRelative("InstanceId").stringValue = instanceId;
            entry.FindPropertyRelative("Coord").vector2IntValue = coord;
            entry.FindPropertyRelative("Cell").objectReferenceValue = definition;
            ResetSite(entry.FindPropertyRelative("Site"));
        }

        private static void ResetSite(SerializedProperty site)
        {
            if (site == null)
                return;

            site.boxedValue = new MapCellSiteConfig { Raid = new RaidSiteConfig() };
        }
    }
}
