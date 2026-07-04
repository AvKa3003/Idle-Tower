using System;
using UnityEngine;

namespace IdleTower.Data.Definitions
{
    [Serializable]
    public struct ResourceCost
    {
        public ResourceDefinition Resource;
        [Min(0)] public int Amount;
    }
}
