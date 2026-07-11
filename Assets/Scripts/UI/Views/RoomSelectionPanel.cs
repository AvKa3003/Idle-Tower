using System;
using System.Collections.Generic;
using IdleTower.Data.Definitions;
using UnityEngine;

namespace IdleTower.UI.Views
{
    public class RoomSelectionPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Transform optionsRoot;
        [SerializeField] private RoomOptionView optionPrefab;

        private readonly List<RoomOptionView> _options = new();

        public event Action<RoomDefinition> RoomSelected;

        private void Awake()
        {
            if (root == null)
                root = gameObject;
        }

        public void Show()
        {
            root.SetActive(true);
        }

        public void Hide()
        {
            root.SetActive(false);
        }

        public void SetOptions(IReadOnlyList<RoomOptionDisplay> displays)
        {
            ClearOptions();

            if (displays == null || optionPrefab == null)
                return;

            var parent = optionsRoot != null ? optionsRoot : transform;

            for (var i = 0; i < displays.Count; i++)
            {
                var option = Instantiate(optionPrefab, parent);
                option.SetDisplay(displays[i]);
                option.Selected += HandleOptionSelected;
                _options.Add(option);
            }
        }

        private void ClearOptions()
        {
            for (var i = 0; i < _options.Count; i++)
            {
                if (_options[i] != null)
                    _options[i].Selected -= HandleOptionSelected;
            }

            for (var i = 0; i < _options.Count; i++)
            {
                if (_options[i] != null)
                    Destroy(_options[i].gameObject);
            }

            _options.Clear();
        }

        private void HandleOptionSelected(RoomDefinition room)
        {
            RoomSelected?.Invoke(room);
        }
    }
}
