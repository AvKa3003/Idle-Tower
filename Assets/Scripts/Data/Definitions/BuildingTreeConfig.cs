using UnityEngine;

namespace IdleTower.Data.Definitions
{
    [CreateAssetMenu(fileName = "BuildingTree", menuName = "IdleTower/Building Tree Config")]
    public class BuildingTreeConfig : ScriptableObject
    {
        [SerializeField] private RoomDefinition[] allRooms;

        public RoomDefinition[] AllRooms => allRooms;
    }
}
