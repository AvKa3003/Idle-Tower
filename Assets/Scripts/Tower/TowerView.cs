using System;
using System.Collections.Generic;
using IdleTower.Data.Definitions;
using IdleTower.Data.Runtime;
using IdleTower.Rooms;
using UnityEngine;

namespace IdleTower.Tower
{
    /// <summary>Контейнер комнат башни. Только отображение TowerState — без игровой логики.</summary>
    public class TowerView : MonoBehaviour
    {
        [SerializeField] private Transform roomsRoot;
        [SerializeField] private EmptyRoomView emptyRoomViewPrefab;
        [SerializeField] private RoomView roomViewPrefab;
        [SerializeField] private float verticalSpacing = 1.5f;

        private readonly List<RoomEntry> _rooms = new();

        public float VerticalSpacing => verticalSpacing;

        public event Action<int> EmptyRoomClicked;
        public event Action<int> RoomClicked;

        /// <summary>Полная пересборка комнат по состоянию башни.</summary>
        public void SyncFromState(TowerState tower)
        {
            ClearRooms();

            if (tower == null || emptyRoomViewPrefab == null || roomViewPrefab == null)
                return;

            var parent = roomsRoot != null ? roomsRoot : transform;

            for (var i = 0; i < tower.Rooms.Count; i++)
            {
                var room = tower.Rooms[i];
                if (room.IsEmpty)
                    AddEmptyRoom(i, parent);
                else
                    AddBuiltRoom(i, room.BuiltRoom, parent);
            }
        }

        /// <summary>Обновить overlay производства на конкретной комнате.</summary>
        public void SetRoomStatus(int roomIndex, RoomStatusInfo info)
        {
            GetRoomView(roomIndex)?.SetStatus(info);
        }

        public RoomView GetRoomView(int roomIndex)
            => FindEntry(roomIndex)?.BuiltRoom;

        private void AddEmptyRoom(int roomIndex, Transform parent)
        {
            var emptyRoom = Instantiate(emptyRoomViewPrefab, parent);
            PlaceRoom(emptyRoom.transform, roomIndex);
            emptyRoom.Initialize(roomIndex);
            emptyRoom.Clicked += HandleEmptyRoomClicked;

            _rooms.Add(new RoomEntry
            {
                RoomIndex = roomIndex,
                Instance = emptyRoom.transform,
                EmptyRoom = emptyRoom
            });
        }

        private void AddBuiltRoom(int roomIndex, RoomDefinition room, Transform parent)
        {
            var roomView = Instantiate(roomViewPrefab, parent);
            PlaceRoom(roomView.transform, roomIndex);
            roomView.Initialize(roomIndex);
            roomView.SetRoom(room);
            roomView.Clicked += HandleRoomClicked;

            _rooms.Add(new RoomEntry
            {
                RoomIndex = roomIndex,
                Instance = roomView.transform,
                BuiltRoom = roomView
            });
        }

        private void PlaceRoom(Transform roomTransform, int roomIndex)
        {
            var y = roomIndex * verticalSpacing;

            if (roomTransform is RectTransform rect)
                rect.anchoredPosition = new Vector2(0f, y);
            else
                roomTransform.localPosition = new Vector3(0f, y, 0f);
        }

        private void ClearRooms()
        {
            for (var i = 0; i < _rooms.Count; i++)
            {
                var entry = _rooms[i];
                if (entry.EmptyRoom != null)
                    entry.EmptyRoom.Clicked -= HandleEmptyRoomClicked;

                if (entry.BuiltRoom != null)
                    entry.BuiltRoom.Clicked -= HandleRoomClicked;

                if (entry.Instance != null)
                    Destroy(entry.Instance.gameObject);
            }

            _rooms.Clear();
        }

        private RoomEntry FindEntry(int roomIndex)
        {
            for (var i = 0; i < _rooms.Count; i++)
            {
                if (_rooms[i].RoomIndex == roomIndex)
                    return _rooms[i];
            }

            return null;
        }

        private void HandleEmptyRoomClicked(int roomIndex)
        {
            EmptyRoomClicked?.Invoke(roomIndex);
        }

        private void HandleRoomClicked(int roomIndex)
        {
            RoomClicked?.Invoke(roomIndex);
        }

        private sealed class RoomEntry
        {
            public int RoomIndex;
            public Transform Instance;
            public EmptyRoomView EmptyRoom;
            public RoomView BuiltRoom;
        }
    }
}
