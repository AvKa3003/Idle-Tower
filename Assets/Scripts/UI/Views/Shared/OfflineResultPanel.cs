using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTower.UI.Views
{
    public class OfflineResultPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI summaryText;

        [Header("Обычные ресурсы")]
        [SerializeField] private Transform resourceRowsRoot;
        [SerializeField] private GameObject resourcesSectionRoot;

        [Header("Юниты (отдельная секция / окно)")]
        [SerializeField] private Transform unitRowsRoot;
        [SerializeField] private GameObject unitsSectionRoot;

        [Header("Общее")]
        [SerializeField] private OfflineResultRowView rowPrefab;
        [SerializeField] private Button closeButton;

        private readonly List<OfflineResultRowView> _resourceRows = new();
        private readonly List<OfflineResultRowView> _unitRows = new();

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

            ClearRows(_resourceRows);
            ClearRows(_unitRows);
        }

        public void Open(
            string summary,
            IReadOnlyList<OfflineResultRowDisplay> resourceDisplays,
            IReadOnlyList<OfflineResultRowDisplay> unitDisplays)
        {
            if (summaryText != null)
                summaryText.text = summary ?? string.Empty;

            if (root != null)
                root.SetActive(false);

            RefreshList(
                resourceDisplays,
                resourceRowsRoot,
                _resourceRows,
                resourcesSectionRoot);

            RefreshList(
                unitDisplays,
                unitRowsRoot,
                _unitRows,
                unitsSectionRoot);

            if (root != null)
                root.SetActive(true);
        }

        private void RefreshList(
            IReadOnlyList<OfflineResultRowDisplay> displays,
            Transform rowsParent,
            List<OfflineResultRowView> rows,
            GameObject sectionRoot)
        {
            var hasRows = displays != null && displays.Count > 0 && rowPrefab != null && rowsParent != null;

            if (sectionRoot != null)
                sectionRoot.SetActive(hasRows);

            if (!hasRows)
            {
                ClearRows(rows);
                return;
            }

            if (rows.Count != displays.Count)
            {
                SetRows(displays, rowsParent, rows);
                return;
            }

            for (var i = 0; i < displays.Count; i++)
            {
                if (rows[i] != null)
                    rows[i].SetDisplay(displays[i]);
            }
        }

        private void SetRows(
            IReadOnlyList<OfflineResultRowDisplay> displays,
            Transform parent,
            List<OfflineResultRowView> rows)
        {
            ClearRows(rows);

            for (var i = 0; i < displays.Count; i++)
            {
                var row = Instantiate(rowPrefab, parent);
                row.SetDisplay(displays[i]);
                rows.Add(row);
            }
        }

        private static void ClearRows(List<OfflineResultRowView> rows)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null)
                    Destroy(rows[i].gameObject);
            }

            rows.Clear();
        }

        private void HandleCloseClick()
        {
            CloseClicked?.Invoke();
        }
    }
}
