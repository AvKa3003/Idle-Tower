using System;
using IdleTower.Data.Definitions;

namespace IdleTower.Editor
{
    /// <summary>
    /// Какая entry Map Editor сейчас показывает в Inspector.
    /// Ключ — instanceId, не индекс массива: Paint/Erase сдвигают индексы.
    /// </summary>
    internal static class MapEditorSelection
    {
        public static MapEditorWindow Window { get; private set; }
        public static MapConfig Config { get; private set; }
        public static string InstanceId { get; private set; } = string.Empty;

        public static event Action Changed;

        public static void Set(MapEditorWindow window, MapConfig config, string instanceId)
        {
            instanceId ??= string.Empty;
            if (ReferenceEquals(Window, window)
                && ReferenceEquals(Config, config)
                && InstanceId == instanceId)
                return;

            Window = window;
            Config = config;
            InstanceId = instanceId;
            Changed?.Invoke();
        }

        public static void Clear()
        {
            if (Window == null && Config == null && string.IsNullOrEmpty(InstanceId))
                return;

            Window = null;
            Config = null;
            InstanceId = string.Empty;
            Changed?.Invoke();
        }
    }
}
