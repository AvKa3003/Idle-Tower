using System.Collections.Generic;
using IdleTower.Data.Definitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTower.UI.Views
{
    public static class UiTextFormat
    {
        public static string FormatCosts(ResourceCost[] costs)
        {
            if (costs == null || costs.Length == 0)
                return string.Empty;

            var parts = new List<string>(costs.Length);
            foreach (var cost in costs)
            {
                if (cost.Resource == null || cost.Amount <= 0)
                    continue;

                var name = string.IsNullOrEmpty(cost.Resource.DisplayName)
                    ? cost.Resource.name
                    : cost.Resource.DisplayName;

                parts.Add($"{name} x{cost.Amount}");
            }

            return string.Join(", ", parts);
        }
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
