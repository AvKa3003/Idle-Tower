using System;
using System.Collections.Generic;
using IdleTower.Data.Definitions;
using IdleTower.Rooms.Behaviors;
using IdleTower.Rooms.Production;

namespace IdleTower.Core
{
    /// <summary>Жёсткая проверка конфигов перед стартом игры. При ошибке — исключение.</summary>
    public static class ConfigValidator
    {
        public static void ValidateOrThrow(GameBalanceConfig balance, BuildingTreeConfig buildingTree)
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
