using System.Collections.Generic;
using IdleTower.Core;
using IdleTower.Data.Definitions;
using IdleTower.Data.Runtime;

namespace IdleTower.Systems
{
    public class UnlockTreeSystem
    {
        private readonly GameServices _services;

        public UnlockTreeSystem(GameServices services)
        {
            _services = services;
        }

        public IReadOnlyList<RoomDefinition> GetAvailableRooms(TowerState tower)
        {
            var result = new List<RoomDefinition>();
            var allRooms = _services.BuildingTree?.AllRooms;
            if (allRooms == null || tower == null)
                return result;

            foreach (var room in allRooms)
            {
                if (room == null)
                    continue;

                if (tower.IsRoomBuilt(room))
                    continue;

                if (!AreUnlockRulesMet(room.UnlockRules, tower))
                    continue;

                result.Add(room);
            }

            return result;
        }

        public bool IsAvailable(RoomDefinition room, TowerState tower)
        {
            if (room == null || tower == null)
                return false;

            if (tower.IsRoomBuilt(room))
                return false;

            return AreUnlockRulesMet(room.UnlockRules, tower);
        }

        public bool AreUnlockRulesMet(UnlockRule[] rules, TowerState tower)
        {
            if (rules == null || rules.Length == 0)
                return false;

            foreach (var rule in rules)
            {
                if (!EvaluateRule(rule, tower))
                    return false;
            }

            return true;
        }

        public bool EvaluateRule(UnlockRule rule, TowerState tower)
        {
            switch (rule.Type)
            {
                case UnlockRuleType.AlwaysAvailable:
                    return true;
                case UnlockRuleType.RequiresRoomBuilt:
                    return rule.RequiredRoom != null && tower.IsRoomBuilt(rule.RequiredRoom);
                default:
                    return false;
            }
        }
    }
}
