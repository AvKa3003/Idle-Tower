using IdleTower.Map;
using UnityEngine;

namespace IdleTower.Data.Definitions
{
    [CreateAssetMenu(fileName = "MapCell", menuName = "IdleTower/Map Cell Definition")]
    public class MapCellDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite sprite;
        [SerializeField] private MapCellBehaviorBase behavior;

        public MapCellId Id => MapCellId.FromSerialized(id);
        public string DisplayName => displayName;
        public Sprite Sprite => sprite;
        public MapCellBehaviorBase Behavior => behavior;
    }
}
