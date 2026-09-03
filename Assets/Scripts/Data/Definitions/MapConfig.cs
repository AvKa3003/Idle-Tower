using System;
using IdleTower.Map;
using UnityEngine;

namespace IdleTower.Data.Definitions
{
    [CreateAssetMenu(fileName = "MapConfig", menuName = "IdleTower/Map Config")]
    public class MapConfig : ScriptableObject
    {
        [SerializeField] private Vector2Int homeCoord;
        [SerializeField] [Min(0)] private int interactionRadius = 1;
        [SerializeField] [Min(0)] private int visionRadius = 2;
        [SerializeField] private MapNeighborMode neighborMode = MapNeighborMode.Four;
        [SerializeField] private MapConfigEntry[] entries = Array.Empty<MapConfigEntry>();

        public Vector2Int HomeCoord => homeCoord;
        public int InteractionRadius => interactionRadius;
        public int VisionRadius => visionRadius;
        public MapNeighborMode NeighborMode => neighborMode;
        public MapConfigEntry[] Entries => entries;
    }

    [Serializable]
    public class MapConfigEntry
    {
        public Vector2Int Coord;
        public MapCellDefinition Cell;

        [Tooltip("Данные экземпляра клетки (Raid / позже другие типы).")]
        public MapCellSiteConfig Site;
    }
}
