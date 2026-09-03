using System;
using System.Collections.Generic;
using IdleTower.Data.Definitions;
using IdleTower.Map;
using IdleTower.Map.Behaviors;
using IdleTower.Rooms.Behaviors;
using IdleTower.Rooms.Production;
using UnityEngine;

namespace IdleTower.Core
{
    /// <summary>Жёсткая проверка конфигов перед стартом игры. При ошибке — исключение.</summary>
    public static class ConfigValidator
    {
        public static void ValidateOrThrow(
            GameBalanceConfig balance,
            BuildingTreeConfig buildingTree,
            MapConfig mapConfig = null)
        {
            var errors = new List<string>();

            if (balance == null)
                errors.Add("GameBalanceConfig не назначен.");
            else
                ValidateBalance(balance, errors);

            if (buildingTree == null)
                errors.Add("BuildingTreeConfig не назначен.");
            else
                ValidateBuildingTree(buildingTree, balance, errors);

            if (mapConfig == null)
                errors.Add("MapConfig не назначен.");
            else
                ValidateMap(mapConfig, balance, errors);

            if (errors.Count == 0)
                return;

            throw new InvalidOperationException(
                "[ConfigValidator] Конфиг невалиден:\n- " + string.Join("\n- ", errors));
        }

        private static void ValidateBalance(GameBalanceConfig balance, List<string> errors)
        {
            if (balance.TicksPerSecond < 1)
                errors.Add("GameBalanceConfig.TicksPerSecond < 1.");

            if (balance.MaxCatchUpSeconds <= 0f)
                errors.Add("GameBalanceConfig.MaxCatchUpSeconds <= 0.");

            var resources = balance.AllResources;
            if (resources == null || resources.Length == 0)
            {
                errors.Add("GameBalanceConfig.AllResources пуст.");
                return;
            }

            var seenIds = new HashSet<string>();
            for (var i = 0; i < resources.Length; i++)
            {
                var resource = resources[i];
                if (resource == null)
                {
                    errors.Add($"GameBalanceConfig.AllResources[{i}] = null.");
                    continue;
                }

                ValidateResourceId(resource, $"GameBalanceConfig.AllResources[{i}]", seenIds, errors);
                ValidateResourceStrength(resource, $"GameBalanceConfig.AllResources[{i}]", errors);
            }

            ValidateCosts(balance.StartingResources, "GameBalanceConfig.StartingResources", seenIds, errors, allowEmptyArray: true);
        }

        private static void ValidateBuildingTree(
            BuildingTreeConfig buildingTree,
            GameBalanceConfig balance,
            List<string> errors)
        {
            var rooms = buildingTree.AllRooms;
            if (rooms == null || rooms.Length == 0)
            {
                errors.Add("BuildingTreeConfig.AllRooms пуст.");
                return;
            }

            var knownResourceIds = CollectResourceIds(balance);
            var roomIds = new HashSet<string>();
            var roomAssets = new HashSet<RoomDefinition>();

            for (var i = 0; i < rooms.Length; i++)
            {
                var room = rooms[i];
                var path = $"BuildingTreeConfig.AllRooms[{i}]";

                if (room == null)
                {
                    errors.Add($"{path} = null.");
                    continue;
                }

                if (!roomAssets.Add(room))
                    errors.Add($"{path}: RoomDefinition '{room.name}' указан в AllRooms повторно.");

                if (room.Id.IsEmpty)
                    errors.Add($"{path} ('{room.name}'): пустой Id.");
                else if (!roomIds.Add(room.Id.Value))
                    errors.Add($"{path} ('{room.name}'): дубликат Room.Id '{room.Id.Value}'.");

                if (room.Behavior == null)
                    errors.Add($"{path} ('{room.name}'): Behavior не назначен.");

                ValidateCosts(room.Cost, $"{path}.Cost", knownResourceIds, errors, allowEmptyArray: true);
                ValidateUnlockRules(room.UnlockRules, $"{path}.UnlockRules", errors, requireAtLeastOne: true);

                if (room.Behavior is ProductionRoomBehavior production)
                    ValidateProductionBehavior(production, knownResourceIds, errors);
            }

            // RequiresRoomBuilt должен ссылаться на комнату из дерева (после сбора всех id)
            for (var i = 0; i < rooms.Length; i++)
            {
                var room = rooms[i];
                if (room?.UnlockRules == null)
                    continue;

                for (var r = 0; r < room.UnlockRules.Length; r++)
                {
                    var rule = room.UnlockRules[r];
                    if (rule.Type != UnlockRuleType.RequiresRoomBuilt)
                        continue;

                    if (rule.RequiredRoom == null)
                        continue; // уже отмечено в ValidateUnlockRules

                    if (!roomAssets.Contains(rule.RequiredRoom))
                    {
                        errors.Add(
                            $"BuildingTreeConfig.AllRooms[{i}].UnlockRules[{r}]: " +
                            $"RequiredRoom '{rule.RequiredRoom.name}' нет в AllRooms.");
                    }
                }
            }
        }

        private static void ValidateMap(
            MapConfig mapConfig,
            GameBalanceConfig balance,
            List<string> errors)
        {
            if (mapConfig.InteractionRadius < 0)
                errors.Add("MapConfig.InteractionRadius < 0.");

            if (mapConfig.VisionRadius < mapConfig.InteractionRadius)
            {
                errors.Add(
                    "MapConfig.VisionRadius < InteractionRadius " +
                    "(видимость должна быть не меньше интеракции).");
            }

            var entries = mapConfig.Entries;
            if (entries == null || entries.Length == 0)
            {
                errors.Add("MapConfig.Entries пуст.");
                return;
            }

            var knownResourceIds = CollectResourceIds(balance);
            var coords = new HashSet<Vector2Int>();
            var homeFound = false;

            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                var path = $"MapConfig.Entries[{i}]";

                if (!coords.Add(entry.Coord))
                    errors.Add($"{path}: дубликат Coord {entry.Coord}.");

                if (entry.Coord == mapConfig.HomeCoord)
                    homeFound = true;

                var cell = entry.Cell;
                if (cell == null)
                {
                    errors.Add($"{path}.Cell = null.");
                    continue;
                }

                if (cell.Id.IsEmpty)
                    errors.Add($"{path} ('{cell.name}'): пустой Cell.Id.");

                if (cell.Behavior == null)
                    errors.Add($"{path} ('{cell.name}'): Behavior не назначен.");
                else if (cell.Behavior is RaidMapCellBehavior)
                    ValidateRaidMapEntry(entries[i], path, knownResourceIds, errors);
            }

            if (!homeFound)
                errors.Add($"MapConfig: нет Entries с Coord == HomeCoord {mapConfig.HomeCoord}.");

            MapCellDefinition homeCell = null;
            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i].Coord == mapConfig.HomeCoord)
                {
                    homeCell = entries[i].Cell;
                    break;
                }
            }

            if (homeCell?.Behavior != null && homeCell.Behavior is not HomeMapCellBehavior)
            {
                errors.Add(
                    "MapConfig: клетка HomeCoord должна иметь HomeMapCellBehavior.");
            }
        }

        private static void ValidateRaidMapEntry(
            MapConfigEntry entry,
            string path,
            HashSet<string> knownResourceIds,
            List<string> errors)
        {
            var raid = entry.Site?.Raid;
            if (raid == null)
            {
                errors.Add($"{path}: Behavior = Raid, но Site.Raid не заполнен.");
                return;
            }

            var raidPath = $"{path}.Site.Raid";
            if (raid.MaxCompletedRaids < 1)
                errors.Add($"{raidPath}: MaxCompletedRaids < 1.");

            ValidateRaidConfig(raid.PreCapture, $"{raidPath}.PreCapture", knownResourceIds, errors);

            // Farm/Passive — поля заложены; полная валидация на этапах E/F.
        }

        private static void ValidateRaidConfig(
            RaidConfig config,
            string path,
            HashSet<string> knownResourceIds,
            List<string> errors)
        {
            if (config == null)
            {
                errors.Add($"{path} = null.");
                return;
            }

            if (config.Duration.TotalSeconds <= 0f)
                errors.Add($"{path}: Duration должен быть > 0.");

            if (config.RequiredStrength < 0)
                errors.Add($"{path}: RequiredStrength < 0.");

            ValidateRaidCosts(config.RequiredUnits, $"{path}.RequiredUnits", knownResourceIds, errors, requireUnit: true);
            ValidateRaidCosts(config.Rewards, $"{path}.Rewards", knownResourceIds, errors, requireUnit: false);
        }

        private static void ValidateRaidCosts(
            ResourceCost[] costs,
            string path,
            HashSet<string> knownResourceIds,
            List<string> errors,
            bool requireUnit)
        {
            if (costs == null)
                return;

            for (var i = 0; i < costs.Length; i++)
            {
                var cost = costs[i];
                var itemPath = $"{path}[{i}]";
                if (cost.Resource == null)
                {
                    errors.Add($"{itemPath}: Resource = null.");
                    continue;
                }

                if (cost.Resource.Id.IsEmpty
                    || (knownResourceIds != null
                        && knownResourceIds.Count > 0
                        && !knownResourceIds.Contains(cost.Resource.Id.Value)))
                {
                    errors.Add($"{itemPath}: ресурс не в GameBalanceConfig.AllResources.");
                }

                if (cost.Amount < 0)
                    errors.Add($"{itemPath}: Amount < 0.");

                if (requireUnit && !cost.Resource.IsUnit)
                    errors.Add($"{itemPath}: RequiredUnits должен быть IsUnit.");
            }
        }

        private static void ValidateProductionBehavior(
            ProductionRoomBehavior production,
            HashSet<string> knownResourceIds,
            List<string> errors)
        {
            var path = $"ProductionRoomBehavior '{production.name}'";
            var modes = production.Modes;

            if (modes == null || modes.Length == 0)
            {
                errors.Add($"{path}: Modes пуст.");
                return;
            }

            var modeIds = new HashSet<string>();
            for (var i = 0; i < modes.Length; i++)
            {
                var mode = modes[i];
                var modePath = $"{path}.Modes[{i}]";

                if (mode == null)
                {
                    errors.Add($"{modePath} = null.");
                    continue;
                }

                if (mode.Id.IsEmpty)
                    errors.Add($"{modePath}: пустой Id.");
                else if (!modeIds.Add(mode.Id.Value))
                    errors.Add($"{modePath}: дубликат Mode.Id '{mode.Id.Value}'.");

                if (mode.CycleDuration.TotalSeconds <= 0f)
                    errors.Add($"{modePath} ('{mode.Id.Value}'): CycleDuration должен быть > 0.");

                ValidateCosts(mode.OutputPerCycle, $"{modePath}.OutputPerCycle", knownResourceIds, errors, allowEmptyArray: true);
                ValidateCosts(mode.InputPerCycle, $"{modePath}.InputPerCycle", knownResourceIds, errors, allowEmptyArray: true);
                ValidateCosts(mode.UnlockCost, $"{modePath}.UnlockCost", knownResourceIds, errors, allowEmptyArray: true);
                ValidateUnlockRules(mode.UnlockRules, $"{modePath}.UnlockRules", errors, requireAtLeastOne: false);
            }
        }

        private static void ValidateUnlockRules(
            UnlockRule[] rules,
            string path,
            List<string> errors,
            bool requireAtLeastOne)
        {
            if (rules == null || rules.Length == 0)
            {
                if (requireAtLeastOne)
                    errors.Add($"{path}: нужен хотя бы один UnlockRule (иначе комната не появится в UI).");
                return;
            }

            for (var i = 0; i < rules.Length; i++)
            {
                var rule = rules[i];
                if (rule.Type == UnlockRuleType.RequiresRoomBuilt && rule.RequiredRoom == null)
                    errors.Add($"{path}[{i}]: RequiresRoomBuilt без RequiredRoom.");
            }
        }

        private static void ValidateCosts(
            ResourceCost[] costs,
            string path,
            HashSet<string> knownResourceIds,
            List<string> errors,
            bool allowEmptyArray)
        {
            if (costs == null || costs.Length == 0)
            {
                if (!allowEmptyArray)
                    errors.Add($"{path} пуст.");
                return;
            }

            for (var i = 0; i < costs.Length; i++)
            {
                var cost = costs[i];
                if (cost.Resource == null)
                {
                    errors.Add($"{path}[{i}]: Resource = null.");
                    continue;
                }

                if (cost.Resource.Id.IsEmpty)
                {
                    errors.Add($"{path}[{i}]: у ресурса '{cost.Resource.name}' пустой Id.");
                    continue;
                }

                if (knownResourceIds != null &&
                    knownResourceIds.Count > 0 &&
                    !knownResourceIds.Contains(cost.Resource.Id.Value))
                {
                    errors.Add(
                        $"{path}[{i}]: ресурс '{cost.Resource.Id.Value}' не входит в GameBalanceConfig.AllResources.");
                }

                if (cost.Amount < 0)
                    errors.Add($"{path}[{i}]: Amount < 0.");
            }
        }

        private static void ValidateResourceId(
            ResourceDefinition resource,
            string path,
            HashSet<string> seenIds,
            List<string> errors)
        {
            if (resource.Id.IsEmpty)
            {
                errors.Add($"{path} ('{resource.name}'): пустой Id.");
                return;
            }

            var id = resource.Id.Value;
            if (!seenIds.Add(id))
                errors.Add($"{path} ('{resource.name}'): дубликат Resource.Id '{id}'.");
        }

        private static void ValidateResourceStrength(
            ResourceDefinition resource,
            string path,
            List<string> errors)
        {
            if (resource.Strength < 0)
            {
                errors.Add(
                    $"{path} ('{resource.name}'): Strength < 0.");
                return;
            }

            if (resource.IsUnit && resource.Strength < 1)
            {
                errors.Add(
                    $"{path} ('{resource.name}'): IsUnit=true, но Strength < 1.");
            }
        }

        private static HashSet<string> CollectResourceIds(GameBalanceConfig balance)
        {
            var set = new HashSet<string>();
            if (balance?.AllResources == null)
                return set;

            foreach (var resource in balance.AllResources)
            {
                if (resource != null && !resource.Id.IsEmpty)
                    set.Add(resource.Id.Value);
            }

            return set;
        }
    }
}
