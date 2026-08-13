using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTower.UI.Views
{
    public class ResourceListRowView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI amountText;

        public void SetDisplay(ResourceListRowDisplay display)
        {
            if (icon != null)
            {
                icon.sprite = display.Icon;
                icon.enabled = display.Icon != null;
            }

            if (nameText != null)
                nameText.text = display.Name ?? string.Empty;

            if (amountText != null)
                amountText.text = display.Amount.ToString();
        }
    }
}
