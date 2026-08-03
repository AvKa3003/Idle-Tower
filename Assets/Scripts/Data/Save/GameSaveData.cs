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
        public int version = 1;
        public long currentTick;
        public float tickAccumulator;
        public long savedUnixTimeUtc;
        public ResourceAmountSave[] resources = Array.Empty<ResourceAmountSave>();
        public TowerRoomSave[] rooms = Array.Empty<TowerRoomSave>();
    }

    /// <summary>Один построенный этаж. Пустой слот сверху в сейв не пишем — восстанавливается при Load.</summary>
    [Serializable]
    public class TowerRoomSave
    {
        public string roomId;
        public string behaviorStateJson;
    }
}
