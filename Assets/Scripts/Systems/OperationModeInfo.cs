using IdleTower.Rooms.Production;

namespace IdleTower.Systems
{
    public class OperationModeInfo
    {
        public ModeId ModeId;
        public OperationMode Mode;
        public bool IsUnlocked;
        public bool IsActive;
        public bool RulesMet;
        public bool CanAffordUnlock;
    }
}
