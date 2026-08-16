using System;
using System.Collections.Generic;
using IdleTower.Map;
using IdleTower.Systems;
using UnityEngine;

namespace IdleTower.UI.Views
{
    /// <summary>Сетка клеток карты. Только отображение — без Systems.</summary>
    public class MapView : MonoBehaviour
    {
        [SerializeField] private RectTransform cellsRoot;
        [SerializeField] private MapCellView cellPrefab;
        [SerializeField] private Sprite fogSprite;
        [SerializeField] private float cellSize = 120f;
        [SerializeField] private float spacing = 8f;

        private readonly List<MapCellView> _cells = new();

        public event Action<Vector2Int> CellClicked;

        public void Sync(IEnumerable<MapCellDisplayInfo> displays)
        {
            Clear();

            if (displays == null || cellPrefab == null)
                return;

            var parent = cellsRoot != null ? cellsRoot : transform as RectTransform;
            var step = cellSize + spacing;

            foreach (var info in displays)
            {
                var isFog = info.Presence == MapPresence.Fog;
                var sprite = isFog ? fogSprite : (info.Sprite != null ? info.Sprite : info.Definition.Sprite);

                var cell = Instantiate(cellPrefab, parent);
                cell.Setup(
                    info.Coord,
                    sprite,
                    info.Presence,
                    info.HasFunctionalClick,
                    !isFog && info.CanClick);
                cell.SetAnchoredPosition(new Vector2(info.Coord.x * step, info.Coord.y * step));
                cell.Clicked += HandleCellClicked;
                _cells.Add(cell);
            }
        }

        public void Clear()
        {
            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                if (cell == null)
                    continue;

                cell.Clicked -= HandleCellClicked;
                Destroy(cell.gameObject);
            }

            _cells.Clear();
        }

        private void HandleCellClicked(Vector2Int coord)
        {
            CellClicked?.Invoke(coord);
        }
    }
}
