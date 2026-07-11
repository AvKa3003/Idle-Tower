using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleTower.UI.Views
{
    public class ProductionModePanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Transform optionsRoot;
        [SerializeField] private OperationOptionView optionPrefab;

        private readonly List<OperationOptionView> _options = new();

        public event Action<int> UnlockClicked;
        public event Action<int> SelectClicked;

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

        public void SetOptions(IReadOnlyList<OperationOptionDisplay> displays)
        {
            ClearOptions();

            if (displays == null || optionPrefab == null)
                return;

            var parent = optionsRoot != null ? optionsRoot : transform;

            for (var i = 0; i < displays.Count; i++)
            {
                var option = Instantiate(optionPrefab, parent);
                option.SetDisplay(displays[i]);
                option.UnlockClicked += HandleUnlockClicked;
                option.SelectClicked += HandleSelectClicked;
                _options.Add(option);
            }
        }

        private void ClearOptions()
        {
            for (var i = 0; i < _options.Count; i++)
            {
                if (_options[i] == null)
                    continue;

                _options[i].UnlockClicked -= HandleUnlockClicked;
                _options[i].SelectClicked -= HandleSelectClicked;
            }

            for (var i = 0; i < _options.Count; i++)
            {
                if (_options[i] != null)
                    Destroy(_options[i].gameObject);
            }

            _options.Clear();
        }

        private void HandleUnlockClicked(int modeIndex)
        {
            UnlockClicked?.Invoke(modeIndex);
        }

        private void HandleSelectClicked(int modeIndex)
        {
            SelectClicked?.Invoke(modeIndex);
        }
    }
}
