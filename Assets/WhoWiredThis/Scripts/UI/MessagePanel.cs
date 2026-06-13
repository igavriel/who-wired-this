using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace WhoWiredThis.UI
{
    public class MessagePanel : MonoBehaviour
    {
        public static MessagePanel Instance { get; private set; }

        [Header("Mode")]
        [Tooltip("When true, registers as the global singleton (legacy UI_Canvas). When false, this is a per-player HUD instance.")]
        [SerializeField] private bool registerAsSingleton = true;

        [Header("References")]
        public GameObject panelRoot;
        public TMP_Text messageText;
        public Button closeButton;
        public TMP_Text closeButtonText;

        private Action onConfirm;
        private Action onCancel;
        private bool isConfirmationVisible;

        /// <summary>Raised when a visible popup is hidden (Close, Action dismiss, confirmation cancel).</summary>
        public event Action PopupHidden;

        void Awake()
        {
            if (registerAsSingleton)
            {
                if (Instance != null && Instance != this)
                {
                    Destroy(gameObject);
                    return;
                }

                Instance = this;
            }

            closeButton?.onClick.AddListener(OnClosePressed);
            panelRoot?.SetActive(false);
        }

        void OnDestroy()
        {
            if (registerAsSingleton && Instance == this)
            {
                Instance = null;
            }
        }

        void Update()
        {
            if (!registerAsSingleton)
            {
                return;
            }

            if (panelRoot == null || !panelRoot.activeSelf)
            {
                return;
            }

            if (isConfirmationVisible)
            {
                if (Input.GetKeyDown(KeyCode.Y) || Input.GetKeyDown(KeyCode.Return))
                {
                    Confirm();
                    return;
                }

                if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.Escape))
                {
                    Cancel();
                }

                return;
            }

            // Space or Enter closes a normal popup.
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                Hide();
            }
        }

        public void Show(string message)
        {
            isConfirmationVisible = false;
            onConfirm = null;
            onCancel = null;
            SetCloseButtonLabel("Close");

            if (messageText != null)
            {
                messageText.text = message;
            }

            panelRoot?.SetActive(true);
        }

        public void ShowConfirmation(string message, Action confirmAction, Action cancelAction = null)
        {
            isConfirmationVisible = true;
            onConfirm = confirmAction;
            onCancel = cancelAction;
            SetCloseButtonLabel("No");

            if (messageText != null)
            {
                messageText.text = $"{message}\n\nYes: Enter / Y\nNo: Esc / N";
            }

            panelRoot?.SetActive(true);
        }

        public void Hide()
        {
            bool wasVisible = IsVisible;
            panelRoot?.SetActive(false);
            if (wasVisible)
            {
                PopupHidden?.Invoke();
            }
        }

        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

        private void OnClosePressed()
        {
            if (isConfirmationVisible)
            {
                Cancel();
                return;
            }

            Hide();
        }

        private void Confirm()
        {
            Action callback = onConfirm;
            Hide();
            callback?.Invoke();
        }

        private void Cancel()
        {
            Action callback = onCancel;
            Hide();
            callback?.Invoke();
        }

        private void SetCloseButtonLabel(string text)
        {
            if (closeButtonText != null)
            {
                closeButtonText.text = text;
                return;
            }

            TMP_Text nestedLabel = closeButton != null ? closeButton.GetComponentInChildren<TMP_Text>() : null;
            if (nestedLabel != null)
            {
                nestedLabel.text = text;
            }
        }
    }
}
