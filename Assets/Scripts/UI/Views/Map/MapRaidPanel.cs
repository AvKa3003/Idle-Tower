using System;
using System.Collections.Generic;
using IdleTower.Data.Definitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTower.UI.Views
{
    /// <summary>Панель набега на клетке карты.</summary>
    public class MapRaidPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text requirementsText;
        [SerializeField] private TMP_Text rewardsText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Transform armyRowsRoot;
        [SerializeField] private MapRaidArmyRowView armyRowPrefab;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Image pauseButtonIcon;
        [SerializeField] private Sprite pauseSprite;
        [SerializeField] private Sprite playSprite;
        [SerializeField] private Button closeButton;

        private readonly List<MapRaidArmyRowView> _armyRows = new();

        public event Action PauseClicked;
        public event Action Closed;
        public event Action<ResourceDefinition, int> ArmyAmountDeltaClicked;

        private void Awake()
        {
            if (root == null)
                root = gameObject;

            if (pauseButton != null)
                pauseButton.onClick.AddListener(HandlePauseClick);

            if (closeButton != null)
                closeButton.onClick.AddListener(HandleCloseClick);

            // Не вызывать Hide() здесь: если GO стартует неактивным, первый Open()
            // включит его → Awake → Hide() сразу закроет панель (нужен второй клик).
            // Закрытое состояние задаётся в сцене (inactive) или через Hide()/Close.
        }

        private void OnDestroy()
        {
            if (pauseButton != null)
                pauseButton.onClick.RemoveListener(HandlePauseClick);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(HandleCloseClick);

            ClearArmyRows();
        }

        public void Open()
        {
            if (root != null)
                root.SetActive(true);

            // Строки могли собрать, пока root был выключен — layout тогда не считался.
            RebuildArmyLayout();
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);

            ClearArmyRows();
        }

        public bool IsOpen => root != null && root.activeSelf;

        public void SetDisplay(MapRaidPanelDisplay display)
        {
            if (titleText != null)
                titleText.text = display.Title ?? string.Empty;

            if (statusText != null)
                statusText.text = display.Status ?? string.Empty;

            if (requirementsText != null)
            {
                requirementsText.gameObject.SetActive(display.ShowRequirements);
                if (display.ShowRequirements)
                    requirementsText.text = display.Requirements ?? string.Empty;
            }

            if (progressText != null)
            {
                progressText.gameObject.SetActive(display.ShowProgress);
                if (display.ShowProgress)
                    progressText.text = display.ProgressLabel ?? string.Empty;
            }

            if (progressSlider != null)
            {
                progressSlider.gameObject.SetActive(display.ShowProgress);
                if (display.ShowProgress)
                {
                    progressSlider.minValue = 0f;
                    progressSlider.maxValue = 1f;
                    progressSlider.value = Mathf.Clamp01(display.Progress01);
                }
            }

            if (rewardsText != null)
            {
                rewardsText.gameObject.SetActive(display.ShowRewards);
                if (display.ShowRewards)
                    rewardsText.text = display.Rewards ?? string.Empty;
            }

            SetPausedVisual(display.IsPaused);

            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(display.ShowPause);
                pauseButton.interactable = display.PauseInteractable;
            }

            if (armyRowsRoot != null)
                armyRowsRoot.gameObject.SetActive(display.ShowArmy);
        }

        public void SetArmyRows(IReadOnlyList<MapRaidArmyRowDisplay> displays)
        {
            if (displays == null || displays.Count == 0 || armyRowPrefab == null)
            {
                ClearArmyRows();
                return;
            }

            if (_armyRows.Count != displays.Count)
                RebuildArmyRows(displays);
            else
            {
                for (var i = 0; i < displays.Count; i++)
                {
                    if (_armyRows[i] != null)
                        _armyRows[i].SetDisplay(displays[i]);
                }
            }

            RebuildArmyLayout();
        }

        public void SetPausedVisual(bool isPaused)
        {
            if (pauseButtonIcon == null)
                return;

            // На паузе — иконка play (нажми чтобы продолжить); в работе — pause.
            pauseButtonIcon.sprite = isPaused ? playSprite : pauseSprite;
            pauseButtonIcon.enabled = pauseButtonIcon.sprite != null;
        }

        private void RebuildArmyRows(IReadOnlyList<MapRaidArmyRowDisplay> displays)
        {
            ClearArmyRows();

            var parent = armyRowsRoot != null ? armyRowsRoot : transform;
            for (var i = 0; i < displays.Count; i++)
            {
                var row = Instantiate(armyRowPrefab, parent);
                row.AmountDeltaClicked += HandleArmyDelta;
                row.SetDisplay(displays[i]);
                _armyRows.Add(row);
            }
        }

        /// <summary>
        /// После Instantiate/SetDisplay ContentSizeFitter и VLG иначе догоняют через кадр–два.
        /// </summary>
        private void RebuildArmyLayout()
        {
            for (var i = 0; i < _armyRows.Count; i++)
            {
                var row = _armyRows[i];
                if (row == null)
                    continue;

                var rowRt = row.transform as RectTransform;
                if (rowRt != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rowRt);
            }

            if (armyRowsRoot is RectTransform armyRt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(armyRt);
        }

        private void ClearArmyRows()
        {
            for (var i = 0; i < _armyRows.Count; i++)
            {
                var row = _armyRows[i];
                if (row == null)
                    continue;

                row.AmountDeltaClicked -= HandleArmyDelta;
                Destroy(row.gameObject);
            }

            _armyRows.Clear();
        }

        private void HandleArmyDelta(ResourceDefinition resource, int delta)
        {
            ArmyAmountDeltaClicked?.Invoke(resource, delta);
        }

        private void HandlePauseClick()
        {
            PauseClicked?.Invoke();
        }

        private void HandleCloseClick()
        {
            Hide();
            Closed?.Invoke();
        }
    }

    public struct MapRaidPanelDisplay
    {
        public string Title;
        public string Status;
        public string Requirements;
        public string Rewards;
        public string ProgressLabel;
        public float Progress01;
        public bool IsPaused;
        public bool PauseInteractable;
        public bool ShowArmy;
        public bool ShowRequirements;
        public bool ShowPause;
        public bool ShowProgress;
        public bool ShowRewards;
    }
}
