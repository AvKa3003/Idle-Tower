using System;
using IdleTower.Rooms.Production;
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

        private ModeId _modeId;

        public event Action<ModeId> UnlockClicked;
        public event Action<ModeId> SelectClicked;

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
            _modeId = display.ModeId;

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
                unlockButton.gameObject.SetActive(display.ShowUnlockButton);
                SetButtonInteractableImmediate(unlockButton, display.ShowUnlockButton && interactable);
            }

            if (selectButton != null)
            {
                selectButton.gameObject.SetActive(display.ShowSelectButton);
                SetButtonInteractableImmediate(selectButton, display.ShowSelectButton && interactable);
            }

            if (canvasGroup != null)
                canvasGroup.alpha = interactable ? 1f : 0.45f;
        }

        private static void SetButtonInteractableImmediate(Button button, bool interactable)
        {
            button.interactable = interactable;

            // Color Tint иначе ~0.1с анимирует из Normal в Disabled — «мигание» кликабельности.
            var graphic = button.targetGraphic;
            if (graphic == null)
                return;

            var colors = button.colors;
            var target = interactable ? colors.normalColor : colors.disabledColor;
            graphic.CrossFadeColor(target, 0f, true, true);
        }

        public void RebuildLayout()
        {
            if (labelText != null)
                labelText.ForceMeshUpdate();

            if (detailText != null)
            {
                detailText.ForceMeshUpdate();
                LayoutRebuilder.ForceRebuildLayoutImmediate(detailText.rectTransform);
            }

            if (transform is RectTransform rectTransform)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        private void HandleUnlockClick()
        {
            UnlockClicked?.Invoke(_modeId);
        }

        private void HandleSelectClick()
        {
            SelectClicked?.Invoke(_modeId);
        }
    }
}
