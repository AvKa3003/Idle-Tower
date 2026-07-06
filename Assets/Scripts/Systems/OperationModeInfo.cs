using IdleTower.Data.Definitions;
using IdleTower.Rooms.Production;

namespace IdleTower.Systems
{
    public class OperationModeInfo
    {
        public int ModeIndex;
        public OperationMode Mode;
        public bool IsUnlocked;
        public bool IsActive;
        public bool CanAffordUnlock;
        public bool RulesMet;
    }
}
