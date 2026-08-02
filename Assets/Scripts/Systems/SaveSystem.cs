using System;
using System.Collections.Generic;
using System.IO;
using IdleTower.Core;
using IdleTower.Data.Definitions;
using IdleTower.Data.Runtime;
using IdleTower.Data.Save;
using UnityEngine;

namespace IdleTower.Systems
{
    /// <summary>
    /// Сохранение / загрузка JSON в persistentDataPath.
    /// Пишет: инвентарь, построенные комнаты (RoomId + behavior JSON), тик.
    /// Behavior JSON уже содержит ActiveModeId, UnlockedModeIds, ElapsedByMode (все режимы).
    /// </summary>
    public class SaveSystem
    {
        public const int CurrentVersion = 1;
        public const string FileName = "idle_tower_save.json";

        private readonly GameServices _services;
        private readonly string _filePath;
        private float _lastSaveUnscaledTime = -999f;
        /// <summary>Максимальный UTC unix, виденный runtime (не откатывается при сдвиге часов назад).</summary>
        private long _maxObservedUnixTimeUtc;

        public SaveSystem(GameServices services)
        {
            _services = services;
            // Unity часто отдаёт persistentDataPath со '/', Path.Combine на Windows — '\';
            // GetFullPath приводит к единому виду для текущей ОС.
            _filePath = Path.GetFullPath(Path.Combine(Application.persistentDataPath, FileName));
        }

        /// <summary>Текущий watermark UTC для офлайна (после Load/Save).</summary>
        public long MaxObservedUnixTimeUtc => _maxObservedUnixTimeUtc;

        public string FilePath => _filePath;

        public bool HasSaveFile => File.Exists(_filePath);

        /// <summary>
        /// Сохранить. minIntervalSeconds &gt; 0 — пропуск, если недавно уже писали (антиспам pause+focus).
        /// force игнорирует интервал.
        /// </summary>
        public bool TrySave(bool force = false, float minIntervalSeconds = 0.5f)
        {
            if (!force && minIntervalSeconds > 0f)
            {
                if (Time.unscaledTime - _lastSaveUnscaledTime < minIntervalSeconds)
                    return false;
            }

            try
            {
                var data = Capture();
                var json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(_filePath, json);
                _lastSaveUnscaledTime = Time.unscaledTime;
                Debug.Log($"[SaveSystem] Сохранено: {_filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Ошибка сохранения: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Загрузить в runtime + офлайн catch-up по watermark (savedUnixTimeUtc).
        /// false — файла нет или ошибка (вызывающий делает NewGame).
        /// </summary>
        public bool TryLoad()
        {
            if (!File.Exists(_filePath))
                return false;

            try
            {
                var json = File.ReadAllText(_filePath);
                var data = JsonUtility.FromJson<GameSaveData>(json);
                if (data == null)
                    throw new InvalidOperationException("пустой JSON.");

                if (data.version != CurrentVersion)
                {
                    throw new InvalidOperationException(
                        $"неподдерживаемая version={data.version}, ожидается {CurrentVersion}.");
                }

                Apply(data);
                _maxObservedUnixTimeUtc = Math.Max(0L, data.savedUnixTimeUtc);
                _services.Offline.ApplyCatchUp(ref _maxObservedUnixTimeUtc);
                Debug.Log($"[SaveSystem] Загружено: {_filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Ошибка загрузки: {ex.Message}");
                return false;
            }
        }

        private GameSaveData Capture()
        {
            var rooms = _services.Tower.Rooms;
            var built = new List<TowerRoomSave>();

            for (var i = 0; i < rooms.Count; i++)
            {
                var slot = rooms[i];
                if (slot.IsEmpty || slot.BuiltRoom == null)
                    continue;

                var behavior = slot.BuiltRoom.Behavior;
                var stateJson = behavior != null
                    ? behavior.SerializeState(slot.State)
                    : string.Empty;

                built.Add(new TowerRoomSave
                {
                    roomId = slot.BuiltRoom.Id.Value,
                    behaviorStateJson = stateJson ?? string.Empty
                });
            }

            var resources = _services.Wallet.CaptureForSave();

            var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            // Watermark только вперёд: откат ОС-часов не затирает максимум.
            _maxObservedUnixTimeUtc = Math.Max(_maxObservedUnixTimeUtc, nowUnix);

            return new GameSaveData
            {
                version = CurrentVersion,
                currentTick = (long)_services.TickSystem.CurrentTick,
                tickAccumulator = _services.TickSystem.Accumulator,
                savedUnixTimeUtc = _maxObservedUnixTimeUtc,
                resources = resources.ToArray(),
                rooms = built.ToArray()
            };
        }

        private void Apply(GameSaveData data)
        {
            var resourceCatalog = BuildResourceCatalog();
            var roomCatalog = BuildRoomCatalog();

            _services.Wallet.ApplyFromSave(
                data.resources ?? Array.Empty<ResourceAmountSave>(),
                resourceCatalog);

            var builtRooms = new List<(RoomDefinition room, RoomBehaviorState state)>();
            var roomSaves = data.rooms ?? Array.Empty<TowerRoomSave>();

            for (var i = 0; i < roomSaves.Length; i++)
            {
                var entry = roomSaves[i];
                var roomId = RoomId.FromSerialized(entry.roomId);
                if (roomId.IsEmpty)
                {
                    throw new InvalidOperationException(
                        $"[SaveSystem] rooms[{i}]: пустой roomId.");
                }

                if (!roomCatalog.TryGetValue(roomId, out var room) || room == null)
                {
                    throw new InvalidOperationException(
                        $"[SaveSystem] rooms[{i}]: неизвестный RoomId '{roomId.Value}'.");
                }

                if (room.Behavior == null)
                {
                    throw new InvalidOperationException(
                        $"[SaveSystem] rooms[{i}]: у '{roomId.Value}' нет Behavior.");
                }

                var state = room.Behavior.DeserializeState(entry.behaviorStateJson);
                builtRooms.Add((room, state));
            }

            _services.Tower.ReplaceWithBuiltRooms(builtRooms);

            var tick = data.currentTick < 0 ? 0UL : (ulong)data.currentTick;
            _services.TickSystem.RestoreFromSave(tick, data.tickAccumulator);
        }

        private Dictionary<ResourceId, ResourceDefinition> BuildResourceCatalog()
        {
            var catalog = new Dictionary<ResourceId, ResourceDefinition>();
            var all = _services.Balance?.AllResources;
            if (all == null)
                return catalog;

            for (var i = 0; i < all.Length; i++)
            {
                var resource = all[i];
                if (resource == null || resource.Id.IsEmpty)
                    continue;

                catalog[resource.Id] = resource;
            }

            return catalog;
        }

        private Dictionary<RoomId, RoomDefinition> BuildRoomCatalog()
        {
            var catalog = new Dictionary<RoomId, RoomDefinition>();
            var all = _services.BuildingTree?.AllRooms;
            if (all == null)
                return catalog;

            for (var i = 0; i < all.Length; i++)
            {
                var room = all[i];
                if (room == null || room.Id.IsEmpty)
                    continue;

                catalog[room.Id] = room;
            }

            return catalog;
        }
    }
}
