using System;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTower.Tower
{
    /// <summary>Пустая комната в башне — слот для строительства.</summary>
    public class EmptyRoomView : MonoBehaviour
    {
        [SerializeField] private Button button;

        public int RoomIndex { get; private set; }

        public event Action<int> Clicked;

        private void Awake()
        {
            if (button == null)
                button = GetComponentInChildren<Button>();

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

        private void HandleClick()
        {
            Clicked?.Invoke(RoomIndex);
        }
    }
}
