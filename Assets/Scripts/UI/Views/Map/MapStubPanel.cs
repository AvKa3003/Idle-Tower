using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTower.UI.Views
{
    /// <summary>Заглушка UI клетки (этап A) до рейда/лута.</summary>
    public class MapStubPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button closeButton;

        public event Action Closed;

        private void Awake()
        {
            if (root == null)
                root = gameObject;

            if (closeButton != null)
                closeButton.onClick.AddListener(HandleCloseClick);

            Hide();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(HandleCloseClick);
        }

        public void Open(string title, string body)
        {
            if (titleText != null)
                titleText.text = title ?? string.Empty;

            if (bodyText != null)
                bodyText.text = body ?? string.Empty;

            if (root != null)
                root.SetActive(true);
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }

        private void HandleCloseClick()
        {
            Hide();
            Closed?.Invoke();
        }
    }
}
