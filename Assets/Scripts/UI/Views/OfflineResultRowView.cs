using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTower.UI.Views
{
    public class OfflineResultRowView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI deltaText;

        public void SetDisplay(OfflineResultRowDisplay display)
        {
            if (icon != null)
            {
                icon.sprite = display.Icon;
                icon.enabled = display.Icon != null;
            }

            if (nameText != null)
                nameText.text = display.Name ?? string.Empty;

            if (deltaText != null)
            {
                var sign = display.Delta > 0 ? "+" : string.Empty;
                deltaText.text = $"{sign}{display.Delta}";
            }
        }
    }
}
