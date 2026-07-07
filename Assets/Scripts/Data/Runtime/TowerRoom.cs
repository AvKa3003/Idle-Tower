using IdleTower.Data.Definitions;

namespace IdleTower.Data.Runtime
{
    /// <summary>Ячейка комнаты в башне: пустая (строительство) или с построенным типом комнаты.</summary>
    public class TowerRoom
    {
        public RoomDefinition BuiltRoom;
        public RoomBehaviorState State;

        public bool IsEmpty => BuiltRoom == null;
    }
}
