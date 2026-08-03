using System;
using System.Collections;
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
        [SerializeField] private Button pauseButton;
        [SerializeField] private Image pauseButtonIcon;
        [SerializeField] private Sprite pauseSprite;
        [SerializeField] private Sprite playSprite;

        private readonly List<OperationOptionView> _options = new();

        public event Action<ModeId> UnlockClicked;
        public event Action<ModeId> SelectClicked;
        public event Action CloseClicked;
        public event Action PauseClicked;

        private void Awake()
        {
            if (root == null)
                root = gameObject;

            if (closeButton != null)
                closeButton.onClick.AddListener(HandleCloseClick);

            if (pauseButton != null)
                pauseButton.onClick.AddListener(HandlePauseClick);
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(HandleCloseClick);

            if (pauseButton != null)
                pauseButton.onClick.RemoveListener(HandlePauseClick);
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

        public void Open(IReadOnlyList<OperationOptionDisplay> displays, bool isPaused)
        {
            // Сначала заполнить при выключенном root — иначе первый кадр
            // показывает префабные кнопки (interactable) до SetDisplay.
            if (root != null)
                root.SetActive(false);

            SetPausedVisual(isPaused);
            RefreshOptions(displays);

            if (root != null)
                root.SetActive(true);

            RebuildOptionsLayout();

            if (isActiveAndEnabled)
            {
                StopCoroutine(nameof(RebuildOptionsLayoutNextFrame));
                StartCoroutine(RebuildOptionsLayoutNextFrame());
            }
        }

        public void SetPausedVisual(bool isPaused)
        {
            if (pauseButtonIcon == null)
                return;

            // На паузе — иконка паузы; когда работает — play.
            pauseButtonIcon.sprite = isPaused ? pauseSprite : playSprite;
            pauseButtonIcon.enabled = pauseButtonIcon.sprite != null;
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

        private IEnumerator RebuildOptionsLayoutNextFrame()
        {
            yield return null;
            RebuildOptionsLayout();
        }

        private void RebuildOptionsLayout()
        {
            for (var i = 0; i < _options.Count; i++)
            {
                if (_options[i] != null)
                    _options[i].RebuildLayout();
            }

            Canvas.ForceUpdateCanvases();

            var parent = optionsRoot != null ? optionsRoot : transform;
            if (parent is RectTransform parentRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);

            if (root != null && root.transform is RectTransform rootRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
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

        private void HandlePauseClick()
        {
            PauseClicked?.Invoke();
        }
    }
}
