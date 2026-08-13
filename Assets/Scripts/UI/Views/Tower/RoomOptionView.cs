using System;
using IdleTower.Data.Definitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTower.UI.Views
{
    public class RoomOptionView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Button selectButton;
        [SerializeField] private CanvasGroup canvasGroup;

        private RoomDefinition _room;

        public event Action<RoomDefinition> Selected;

        private void Awake()
        {
            if (selectButton == null)
                selectButton = GetComponentInChildren<Button>();

            if (selectButton != null)
                selectButton.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (selectButton != null)
                selectButton.onClick.RemoveListener(HandleClick);
        }

        public void SetDisplay(RoomOptionDisplay display)
        {
            _room = display.Room;
            var room = display.Room;

            if (room == null)
                return;

            if (icon != null)
            {
                icon.sprite = room.Icon;
                icon.enabled = room.Icon != null;
            }

            if (nameText != null)
                nameText.text = string.IsNullOrEmpty(room.DisplayName) ? room.name : room.DisplayName;

            if (costText != null)
                costText.text = display.CostText ?? string.Empty;

            SetAffordable(display.CanAfford);
        }

        public void SetAffordable(bool canAfford)
        {
            if (selectButton != null)
                selectButton.interactable = canAfford;

            if (canvasGroup != null)
                canvasGroup.alpha = canAfford ? 1f : 0.45f;
        }

        private void HandleClick()
        {
            if (_room != null)
                Selected?.Invoke(_room);
        }
    }
}
