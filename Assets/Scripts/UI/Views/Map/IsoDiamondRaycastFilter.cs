using UnityEngine;

namespace IdleTower.UI.Views
{
    /// <summary>
    /// Hit-test по изометрическому ромбу, вписанному в RectTransform
    /// (|dx|/(w/2) + |dy|/(h/2) ≤ 1). Вешать на тот же GO, что Graphic (кнопка клетки).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class IsoDiamondRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        private RectTransform _rect;

        private void Awake()
        {
            _rect = transform as RectTransform;
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (_rect == null)
                _rect = transform as RectTransform;

            if (_rect == null)
                return false;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rect, screenPoint, eventCamera, out var local))
                return false;

            var r = _rect.rect;
            var halfW = r.width * 0.5f;
            var halfH = r.height * 0.5f;
            if (halfW <= 0f || halfH <= 0f)
                return false;

            var dx = Mathf.Abs(local.x - r.center.x) / halfW;
            var dy = Mathf.Abs(local.y - r.center.y) / halfH;
            return dx + dy <= 1f;
        }
    }
}
