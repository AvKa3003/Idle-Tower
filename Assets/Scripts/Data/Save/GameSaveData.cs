using System;
using IdleTower.Data.Runtime;

namespace IdleTower.Data.Save
{
    /// <summary>
    /// Корневой DTO сейва (JsonUtility).
    /// Прогресс режимов целиком в behaviorStateJson (ActiveModeId, Unlocked, ElapsedByMode по всем ModeId).
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        public int version = 2;
        public long currentTick;
        public float tickAccumulator;
        public long savedUnixTimeUtc;
        public ResourceAmountSave[] resources = Array.Empty<ResourceAmountSave>();
        public TowerRoomSave[] rooms = Array.Empty<TowerRoomSave>();
        public MapCellSave[] mapCells = Array.Empty<MapCellSave>();
    }

    /// <summary>BehaviorState одной клетки карты (ключ — Coord).</summary>
    [Serializable]
    public class MapCellSave
    {
        public int x;
        public int y;
        public string behaviorType;
        public string behaviorStateJson;
        public ResourceAmountSave[] activeRaidRewards = Array.Empty<ResourceAmountSave>();
        public string savedConfigFingerprint;
        /// <summary>PostCaptureMode на момент сейва (для детекта смены mode).</summary>
        public int savedPostCaptureMode;
    }

    /// <summary>Один построенный этаж. Пустой слот сверху в сейв не пишем — восстанавливается при Load.</summary>
    [Serializable]
    public class TowerRoomSave
    {
        public string roomId;
        public string behaviorStateJson;
    }
}
