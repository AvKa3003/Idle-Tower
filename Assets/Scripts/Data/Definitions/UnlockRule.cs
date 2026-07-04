using System;
using UnityEngine;

namespace IdleTower.Data.Definitions
{
    [Serializable]
    public struct UnlockRule
    {
        public UnlockRuleType Type;
        public RoomDefinition RequiredRoom;
    }
}
