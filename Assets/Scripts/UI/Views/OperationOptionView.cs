using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTower.UI.Views
{
    public class OperationOptionView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private TextMeshProUGUI detailText;
        [SerializeField] private Button unlockButton;
        [SerializeField] private Button selectButton;
        [SerializeField] private CanvasGroup canvasGroup;

        private int _modeIndex;

        public event Action<int> UnlockClicked;
        public event Action<int> SelectClicked;

        private void Awake()
        {
            if (unlockButton != null)
                unlockButton.onClick.AddListener(HandleUnlockClick);

            if (selectButton != null)
                selectButton.onClick.AddListener(HandleSelectClick);
        }

        private void OnDestroy()
        {
            if (unlockButton != null)
                unlockButton.onClick.RemoveListener(HandleUnlockClick);

            if (selectButton != null)
                selectButton.onClick.RemoveListener(HandleSelectClick);
        }

        public void SetDisplay(OperationOptionDisplay display)
        {
            _modeIndex = display.ModeIndex;

            if (labelText != null)
            {
                var prefix = display.IsActive ? "> " : string.Empty;
                labelText.text = prefix + (display.Label ?? string.Empty);
            }

            if (detailText != null)
                detailText.text = display.DetailText ?? string.Empty;

            var interactable = display.Interactable;

            if (unlockButton != null)
            {
                unlockButton.interactable = display.ShowUnlockButton && interactable;
                unlockButton.gameObject.SetActive(display.ShowUnlockButton);
            }

            if (selectButton != null)
            {
                selectButton.interactable = display.ShowSelectButton && interactable;
                selectButton.gameObject.SetActive(display.ShowSelectButton);
            }

            if (canvasGroup != null)
                canvasGroup.alpha = interactable ? 1f : 0.45f;
        }

        private void HandleUnlockClick()
        {
            UnlockClicked?.Invoke(_modeIndex);
        }

        private void HandleSelectClick()
        {
            SelectClicked?.Invoke(_modeIndex);
        }
    }
}
