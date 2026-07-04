using IdleTower.Data.Definitions;
using UnityEngine;

namespace IdleTower.Rooms
{
    public class RoomStatusInfo
    {
        public Sprite OutputIcon;
        public string ModeLabel;
        public int AmountPerCycle;
        public ResourceCost[] OutputPerCycle;
        public float Progress01;
        public float ElapsedSeconds;
        public float CycleTotalSeconds;
        public bool WaitingForInput;
    }
}
