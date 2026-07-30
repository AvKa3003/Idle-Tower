using System;
using IdleTower.Data.Definitions;
using IdleTower.Rooms;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTower.Tower
{
    /// <summary>Построенная комната в башне + overlay статуса производства.</summary>
    public class RoomView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Transform roomVisualRoot;
        [SerializeField] private Image roomIconFallback;
        [SerializeField] private GameObject statusOverlay;
        [SerializeField] private Image progressFill;
        [SerializeField] private Image outputIcon;
        [SerializeField] private TextMeshProUGUI modeLabelText;
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private GameObject waitingForInputIndicator;

        private GameObject _spawnedVisual;

        public int RoomIndex { get; private set; }

        public event Action<int> Clicked;

        private void Awake()
        {
            if (button == null)
                button = GetComponentInChildren<Button>();

            if (roomVisualRoot == null)
                roomVisualRoot = transform;

            if (button != null)
                button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }

        public void Initialize(int roomIndex)
        {
            RoomIndex = roomIndex;
        }

        public void SetRoom(RoomDefinition room)
        {
            ClearVisual();

            if (room == null)
                return;

            if (room.Prefab != null)
            {
                _spawnedVisual = Instantiate(room.Prefab, roomVisualRoot);
                _spawnedVisual.transform.localPosition = Vector3.zero;
                _spawnedVisual.transform.localRotation = Quaternion.identity;
                _spawnedVisual.transform.localScale = Vector3.one;

                if (roomIconFallback != null)
                    roomIconFallback.enabled = false;
            }
            else if (roomIconFallback != null)
            {
                roomIconFallback.enabled = true;
                roomIconFallback.sprite = room.Icon;
                roomIconFallback.color = room.Icon != null ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            }
        }

        public void SetStatus(RoomStatusInfo info)
        {
            if (info == null || info.CycleTotalSeconds <= 0f)
            {
                SetOverlayVisible(false);
                return;
            }

            SetOverlayVisible(true);

            if (progressFill != null)
                progressFill.fillAmount = Mathf.Clamp01(info.Progress01);

            if (outputIcon != null)
            {
                outputIcon.sprite = info.OutputIcon;
                outputIcon.enabled = info.OutputIcon != null;
            }

            if (modeLabelText != null)
                modeLabelText.text = info.ModeLabel ?? string.Empty;

            if (amountText != null)
                amountText.text = info.CycleSummary ?? string.Empty;

            if (timeText != null)
            {
                timeText.text = info.CycleTotalSeconds > 0f
                    ? $"{info.ElapsedSeconds:F1} / {info.CycleTotalSeconds:F0}"
                    : string.Empty;
            }

            if (waitingForInputIndicator != null)
                waitingForInputIndicator.SetActive(info.WaitingForInput);
        }

        private void SetOverlayVisible(bool visible)
        {
            if (statusOverlay != null)
                statusOverlay.SetActive(visible);
        }

        private void ClearVisual()
        {
            if (_spawnedVisual != null)
            {
                Destroy(_spawnedVisual);
                _spawnedVisual = null;
            }

            if (roomIconFallback != null)
            {
                roomIconFallback.sprite = null;
                roomIconFallback.enabled = false;
            }
        }

        private void HandleClick()
        {
            Clicked?.Invoke(RoomIndex);
        }
    }
}
