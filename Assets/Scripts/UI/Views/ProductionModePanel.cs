using System;
using System.Collections.Generic;
using IdleTower.Rooms.Production;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTower.UI.Views
{
    public class ProductionModePanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Transform optionsRoot;
        [SerializeField] private OperationOptionView optionPrefab;
        [SerializeField] private Button closeButton;

        private readonly List<OperationOptionView> _options = new();

        public event Action<ModeId> UnlockClicked;
        public event Action<ModeId> SelectClicked;
        public event Action CloseClicked;

        private void Awake()
        {
            if (root == null)
                root = gameObject;

            if (closeButton != null)
                closeButton.onClick.AddListener(HandleCloseClick);
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(HandleCloseClick);
        }

        public void Show()
        {
            root.SetActive(true);
        }

        public void Hide()
        {
            root.SetActive(false);
            ClearOptions();
        }

        public void Open(IReadOnlyList<OperationOptionDisplay> displays)
        {
            if (root != null)
                root.SetActive(false);

            RefreshOptions(displays);

            if (root != null)
                root.SetActive(true);
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

        public void RefreshOptions(IReadOnlyList<OperationOptionDisplay> displays)
        {
            if (displays == null || optionPrefab == null)
                return;

            if (_options.Count != displays.Count)
            {
                SetOptions(displays);
                return;
            }

            for (var i = 0; i < displays.Count; i++)
            {
                if (_options[i] != null)
                    _options[i].SetDisplay(displays[i]);
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

        private void HandleUnlockClicked(ModeId modeId)
        {
            UnlockClicked?.Invoke(modeId);
        }

        private void HandleSelectClicked(ModeId modeId)
        {
            SelectClicked?.Invoke(modeId);
        }

        private void HandleCloseClick()
        {
            CloseClicked?.Invoke();
        }
    }
}
