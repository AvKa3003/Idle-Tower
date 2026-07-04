using IdleTower.Core;
using IdleTower.Data.Runtime;

namespace IdleTower.Rooms
{
    public interface IRoomBehavior
    {
        void OnRoomBuilt(RoomBehaviorContext context);
        RoomClickResult OnRoomClicked(RoomBehaviorContext context);
        void Tick(RoomBehaviorContext context, TickContext tickContext);
        RoomBehaviorState CreateDefaultState();
        string SerializeState(RoomBehaviorState state);
        RoomBehaviorState DeserializeState(string json);
        RoomStatusInfo GetRoomStatusInfo(RoomBehaviorContext context);
    }
}
