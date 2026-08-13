using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTower.UI.Views
{
    public class ResourceListPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Transform rowsRoot;
        [SerializeField] private ResourceListRowView rowPrefab;
        [SerializeField] private Button closeButton;

        private readonly List<ResourceListRowView> _rows = new();

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

        public void Open(IReadOnlyList<ResourceListRowDisplay> displays)
        {
            if (root != null)
                root.SetActive(false);

            RefreshRows(displays);

            if (root != null)
                root.SetActive(true);
        }

        public void RefreshRows(IReadOnlyList<ResourceListRowDisplay> displays)
        {
            if (displays == null || rowPrefab == null)
                return;

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

        private void SetRows(IReadOnlyList<ResourceListRowDisplay> displays)
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
