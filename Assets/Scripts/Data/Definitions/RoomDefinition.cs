using IdleTower.Rooms;
using UnityEngine;

namespace IdleTower.Data.Definitions
{
    [CreateAssetMenu(fileName = "Room", menuName = "IdleTower/Room Definition")]
    public class RoomDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private GameObject prefab;
        [SerializeField] private ResourceCost[] cost;
        [SerializeField] private RoomBehaviorBase behavior;
        [SerializeField] private UnlockRule[] unlockRules;

        public RoomId Id => RoomId.FromSerialized(id);
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public GameObject Prefab => prefab;
        public ResourceCost[] Cost => cost;
        public RoomBehaviorBase Behavior => behavior;
        public UnlockRule[] UnlockRules => unlockRules;
    }
}
