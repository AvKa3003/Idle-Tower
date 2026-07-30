using IdleTower.Core;
using IdleTower.Data.Definitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTower.UI.Views
{
    public static class UiTextFormat
    {
        public static string FormatCosts(ResourceCost[] costs)
            => ResourceTextFormat.FormatCosts(costs);
    }

    public class ResourceSlotView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI amountText;

        public void SetResource(ResourceDefinition resource, int amount)
        {
            if (icon != null)
            {
                icon.sprite = resource?.Icon;
                icon.enabled = resource?.Icon != null;
            }

            if (amountText != null)
                amountText.text = amount.ToString();
        }
    }
}
