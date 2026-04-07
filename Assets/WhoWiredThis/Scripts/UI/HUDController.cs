using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WhoWiredThis.Core;

namespace WhoWiredThis.UI
{
    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance { get; private set; }

        [Header("Top Bar")]
        public TMP_Text roomNameText;
        public TMP_Text scoreText;
        public TMP_Text timerText;
        public TMP_Text interactPromptText;

        [Header("Buttons")]
        public Button bagButton;
        public Button terminalButton;

        [Header("Panels")]
        public GameObject inventoryPanel;

        private bool inventoryVisible;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void Start()
        {
            bagButton?.onClick.AddListener(ToggleInventory);
            terminalButton?.onClick.AddListener(
                () => MessagePanel.Instance?.Show("Terminal is offline.\nCheck the relay first."));

            ScoreManager.Instance.OnScoreChanged += RefreshScore;
            TimerManager.Instance.OnTimerUpdated  += RefreshTimer;
            GameManager.Instance.OnZoneChanged    += RefreshZone;

            RefreshScore(ScoreManager.Instance.CurrentScore);
            RefreshZone(GameManager.Instance.currentZoneName);

            inventoryPanel?.SetActive(false);
            SetInteractPrompt(null);
        }

        void RefreshScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}/{ScoreManager.MaxScore}";
            }
        }

        void RefreshTimer(float seconds)
        {
            if (timerText == null)
            {
                return;
            }

            int m = (int)seconds / 60;
            int s = (int)seconds % 60;
            timerText.text = $"{m:00}:{s:00}";
        }

        void RefreshZone(string zoneName)
        {
            if (roomNameText != null)
            {
                roomNameText.text = zoneName;
            }
        }

        public void SetInteractPrompt(string text)
        {
            if (interactPromptText == null)
            {
                return;
            }

            bool hasText = !string.IsNullOrEmpty(text);
            interactPromptText.gameObject.SetActive(hasText);

            if (hasText)
            {
                interactPromptText.text = text;
            }
        }

        public void ToggleInventory()
        {
            inventoryVisible = !inventoryVisible;
            inventoryPanel?.SetActive(inventoryVisible);
        }
    }
}
