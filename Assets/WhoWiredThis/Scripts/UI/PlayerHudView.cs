using TMPro;
using UnityEngine;

namespace WhoWiredThis.UI
{
    public class PlayerHudView : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private TMP_Text roomNameText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text timerText;

        [Header("Interact Prompt")]
        [SerializeField] private TMP_Text interactPromptText;

        [Header("Popup")]
        [SerializeField] private MessagePanel messagePanel;

        [Header("Scene transition")]
        [SerializeField] private SceneTransitionFadeOverlay fadeOverlay;

        public bool IsPopupOpen
        {
            get
            {
                CacheMessagePanelFromHierarchy();
                return messagePanel != null && messagePanel.IsVisible;
            }
        }

        public MessagePanel MessagePanel
        {
            get
            {
                CacheMessagePanelFromHierarchy();
                return messagePanel;
            }
        }

        public SceneTransitionFadeOverlay FadeOverlay
        {
            get
            {
                CacheFadeOverlayFromHierarchy();
                return fadeOverlay;
            }
        }

        void Awake()
        {
            CacheTopBarTextsFromHierarchy();
            CacheMessagePanelFromHierarchy();
            CacheFadeOverlayFromHierarchy();
        }

        public void ApplySharedHudState(string roomName, string scoreLine, string timeLine)
        {
            CacheTopBarTextsFromHierarchy();

            if (roomNameText != null)
            {
                roomNameText.text = roomName;
            }
            else
            {
                Debug.LogWarning("[PlayerHudView] roomNameText is not assigned.", this);
            }

            if (scoreText != null)
            {
                scoreText.text = scoreLine;
            }
            else
            {
                Debug.LogWarning("[PlayerHudView] scoreText is not assigned.", this);
            }

            if (timerText != null)
            {
                timerText.text = timeLine;
            }
            else
            {
                Debug.LogWarning("[PlayerHudView] timerText is not assigned.", this);
            }
        }

        public void SetInteractPrompt(string text)
        {
            if (interactPromptText == null)
            {
                if (!string.IsNullOrEmpty(text))
                {
                    Debug.LogWarning("[PlayerHudView] interactPromptText is not assigned.", this);
                }

                return;
            }

            bool hasText = !string.IsNullOrEmpty(text);
            interactPromptText.gameObject.SetActive(hasText);

            if (hasText)
            {
                interactPromptText.text = text;
            }
        }

        public void ClearInteractPrompt()
        {
            SetInteractPrompt(null);
        }

        public void ShowPopup(string message)
        {
            CacheMessagePanelFromHierarchy();

            if (messagePanel == null)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    Debug.LogWarning("[PlayerHudView] messagePanel is not assigned.", this);
                }

                return;
            }

            messagePanel.Show(message);
        }

        public void HidePopup()
        {
            CacheMessagePanelFromHierarchy();
            messagePanel?.Hide();
        }

        private void CacheMessagePanelFromHierarchy()
        {
            if (messagePanel != null)
            {
                return;
            }

            messagePanel = GetComponentInChildren<MessagePanel>(true);
        }

        private void CacheFadeOverlayFromHierarchy()
        {
            if (fadeOverlay != null)
            {
                return;
            }

            fadeOverlay = GetComponent<SceneTransitionFadeOverlay>();
            if (fadeOverlay == null)
            {
                fadeOverlay = gameObject.AddComponent<SceneTransitionFadeOverlay>();
            }
        }

        private void CacheTopBarTextsFromHierarchy()
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                switch (text.name)
                {
                    case "RoomNameText":
                        roomNameText = text;
                        break;
                    case "ScoreText":
                        scoreText = text;
                        break;
                    case "TimerText":
                        timerText = text;
                        break;
                }
            }
        }
    }
}
