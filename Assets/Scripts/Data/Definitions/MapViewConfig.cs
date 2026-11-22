using IdleTower.Map;
using UnityEngine;

namespace IdleTower.Data.Definitions
{
    /// <summary>
    /// Шаг сетки и масштаб карты. MapView и Map Editor читают один и тот же ассет.
    /// </summary>
    [CreateAssetMenu(fileName = "MapViewConfig", menuName = "IdleTower/Map View Config")]
    public class MapViewConfig : ScriptableObject
    {
        [Header("Основание ромба (пиксели текстуры)")]
        [Tooltip("Ширина ромба основания клетки (= ширина спрайта земли в px).")]
        [SerializeField] private float baseWidth = 78f;

        [Tooltip("Высота ромба основания (= высота спрайта земли в px). Обычно baseWidth/2 для 2:1.")]
        [SerializeField] private float baseHeight = 39f;

        [Tooltip(
            "Зазор между основаниями в пикселях. 1 = 1px щель (целиком в half-step, без /2); 0 = вплотную.")]
        [SerializeField] private float spacingPixels;

        [Header("Подгонка шага (pixel-art)")]
        [Tooltip("Добавка к half-step по X: baseWidth/2 + spacing + adjust.")]
        [SerializeField] private float stepAdjustX = 0.5f;

        [Tooltip("Добавка к half-step по Y: baseHeight/2 + spacing + adjust.")]
        [SerializeField] private float stepAdjustY = 0.5f;

        [Header("Масштаб")]
        [Tooltip("Стартовый масштаб сетки. Колесо в игре и зум превью его не перезаписывают.")]
        [SerializeField] private float mapScale = 4f;

        [SerializeField] private float mapScaleMin = 4f;

        [SerializeField] private float mapScaleMax = 8f;

        public float BaseWidth => baseWidth;
        public float BaseHeight => baseHeight;
        public float SpacingPixels => spacingPixels;
        public float StepAdjustX => stepAdjustX;
        public float StepAdjustY => stepAdjustY;
        public float MapScale => mapScale;
        public float MapScaleMin => mapScaleMin;
        public float MapScaleMax => mapScaleMax;

        public float HalfStepX => IsoMapLayout.HalfStep(baseWidth, spacingPixels, stepAdjustX);
        public float HalfStepY => IsoMapLayout.HalfStep(baseHeight, spacingPixels, stepAdjustY);
    }
}
