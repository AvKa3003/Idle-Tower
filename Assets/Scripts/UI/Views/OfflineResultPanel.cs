using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTower.UI.Views
{
    /// <summary>Модалка итогов офлайн-симуляции: сводка + строки дельт ресурсов.</summary>
    public class OfflineResultPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI summaryText;
        [SerializeField] private Transform rowsRoot;
        [SerializeField] private OfflineResultRowView rowPrefab;
        [SerializeField] private Button closeButton;

        private readonly List<OfflineResultRowView> _rows = new();

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

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);

            ClearRows();
        }

        public void Open(string summary, IReadOnlyList<OfflineResultRowDisplay> displays)
        {
            if (summaryText != null)
                summaryText.text = summary ?? string.Empty;

            if (root != null)
                root.SetActive(false);

            RefreshRows(displays);

            if (root != null)
                root.SetActive(true);
        }

        public void RefreshRows(IReadOnlyList<OfflineResultRowDisplay> displays)
        {
            if (displays == null || rowPrefab == null)
            {
                ClearRows();
                return;
            }

            if (_rows.Count != displays.Count)
            {
                SetRows(displays);
                return;
            }

            for (var i = 0; i < displays.Count; i++)
            {
                if (_rows[i] != null)
                    _rows[i].SetDisplay(displays[i]);
            }
        }

        private void SetRows(IReadOnlyList<OfflineResultRowDisplay> displays)
        {
            ClearRows();

            if (displays == null || rowPrefab == null)
                return;

            var parent = rowsRoot != null ? rowsRoot : transform;

            for (var i = 0; i < displays.Count; i++)
            {
                var row = Instantiate(rowPrefab, parent);
                row.SetDisplay(displays[i]);
                _rows.Add(row);
            }
        }

        private void ClearRows()
        {
            for (var i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] != null)
                    Destroy(_rows[i].gameObject);
            }

            _rows.Clear();
        }

        private void HandleCloseClick()
        {
            CloseClicked?.Invoke();
        }
    }
}
