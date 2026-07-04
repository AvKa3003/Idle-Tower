using IdleTower.Core;
using IdleTower.Data.Runtime;
using UnityEngine;

namespace IdleTower.Rooms
{
    public abstract class RoomBehaviorBase : ScriptableObject, IRoomBehavior
    {
        public abstract void OnRoomBuilt(RoomBehaviorContext context);
        public abstract RoomClickResult OnRoomClicked(RoomBehaviorContext context);
        public abstract void Tick(RoomBehaviorContext context, TickContext tickContext);
        public abstract RoomBehaviorState CreateDefaultState();
        public abstract string SerializeState(RoomBehaviorState state);
        public abstract RoomBehaviorState DeserializeState(string json);
        public abstract RoomStatusInfo GetRoomStatusInfo(RoomBehaviorContext context);
    }
}
