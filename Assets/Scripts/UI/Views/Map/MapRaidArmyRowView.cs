using System;
using IdleTower.Data.Definitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTower.UI.Views
{
    public class MapRaidArmyRowView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private Button minusButton;
        [SerializeField] private Button plusButton;

        private ResourceDefinition _resource;

        public event Action<ResourceDefinition, int> AmountDeltaClicked;

        private void Awake()
        {
            if (minusButton != null)
                minusButton.onClick.AddListener(HandleMinus);

            if (plusButton != null)
                plusButton.onClick.AddListener(HandlePlus);
        }

        private void OnDestroy()
        {
            if (minusButton != null)
                minusButton.onClick.RemoveListener(HandleMinus);

            if (plusButton != null)
                plusButton.onClick.RemoveListener(HandlePlus);
        }

        public void SetDisplay(MapRaidArmyRowDisplay display)
        {
            _resource = display.Resource;

            if (icon != null)
            {
                icon.sprite = display.Icon;
                icon.enabled = display.Icon != null;
            }

            if (nameText != null)
            {
                var strength = display.StrengthPerUnit > 0
                    ? $" (сила {display.StrengthPerUnit})"
                    : string.Empty;
                nameText.text = (display.Name ?? string.Empty) + strength;
            }

            if (amountText != null)
                amountText.text = $"{display.PlannedAmount} / {display.WalletAmount}";
        }

        private void HandleMinus()
        {
            if (_resource != null)
                AmountDeltaClicked?.Invoke(_resource, -1);
        }

        private void HandlePlus()
        {
            if (_resource != null)
                AmountDeltaClicked?.Invoke(_resource, +1);
        }
    }
}
