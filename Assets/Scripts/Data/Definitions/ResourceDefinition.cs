using UnityEngine;

namespace IdleTower.Data.Definitions
{
    [CreateAssetMenu(fileName = "Resource", menuName = "IdleTower/Resource Definition")]
    public class ResourceDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;

        public ResourceId Id => ResourceId.FromSerialized(id);
        public string DisplayName => displayName;
        public Sprite Icon => icon;
    }
}
