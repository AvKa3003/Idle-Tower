using UnityEngine;

namespace IdleTower.Map
{
    /// <summary>
    /// Чистая iso-раскладка сетки (игра и превью редактора).
    /// Логические Coord не меняются — только экранные позиции и порядок отрисовки.
    /// </summary>
    public static class IsoMapLayout
    {
        /// <summary>
        /// Half-step по одной оси: base/2 + spacing целиком + adjust.
        /// spacingPixels = 1 → 1px щель между основаниями (не /2).
        /// </summary>
        public static float HalfStep(float baseSize, float spacingPixels, float stepAdjust)
            => Mathf.Max(0f, baseSize * 0.5f + spacingPixels + stepAdjust);

        /// <summary>
        /// Изометрия: X вправо-вниз по сетке, Y влево-вниз (UI Y вверх).
        /// </summary>
        public static Vector2 CoordToAnchored(Vector2Int coord, float halfStepX, float halfStepY)
        {
            return new Vector2(
                (coord.x - coord.y) * halfStepX,
                (coord.x + coord.y) * halfStepY);
        }

        /// <summary>
        /// Обратное к CoordToAnchored: content → ближайший logical coord (для hit-test в редакторе).
        /// </summary>
        public static Vector2Int AnchoredToCoord(Vector2 anchored, float halfStepX, float halfStepY)
        {
            var u = halfStepX > 0.0001f ? anchored.x / halfStepX : 0f;
            var v = halfStepY > 0.0001f ? anchored.y / halfStepY : 0f;
            return new Vector2Int(
                Mathf.RoundToInt((u + v) * 0.5f),
                Mathf.RoundToInt((-u + v) * 0.5f));
        }

        /// <summary>
        /// Дальние клетки (больший x+y) раньше в сортировке → ближние сверху (перекрытия).
        /// Тай-брейк: x, затем y.
        /// </summary>
        public static int CompareDepthBackToFront(Vector2Int a, Vector2Int b)
        {
            // Больший x+y = дальше «на север» экрана = позади.
            var depthA = a.x + a.y;
            var depthB = b.x + b.y;
            var byDepth = depthB.CompareTo(depthA);
            if (byDepth != 0)
                return byDepth;

            // Стабильный тай-брейк.
            byDepth = a.x.CompareTo(b.x);
            return byDepth != 0 ? byDepth : a.y.CompareTo(b.y);
        }
    }
}
