using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        public Button menuButton;
        public Button soundToggleButton;
        public Button restartButton;
        public Button helpMenuButton;
        public Button aboutMenuButton;
        public TMP_Text soundButtonLabel;

        [Header("Panels")]
        public GameObject inventoryPanel;
        public GameObject menuPanel;

        private bool inventoryVisible;
        private bool soundEnabled = true;

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
            menuButton?.onClick.AddListener(ToggleHamburgerMenu);
            soundToggleButton?.onClick.AddListener(ToggleSound);
            restartButton?.onClick.AddListener(RequestRestart);
            helpMenuButton?.onClick.AddListener(ToggleHelp);
            aboutMenuButton?.onClick.AddListener(ShowAbout);

            ScoreManager.Instance.OnScoreChanged += RefreshScore;
            TimerManager.Instance.OnTimerUpdated  += RefreshTimer;
            GameManager.Instance.OnZoneChanged    += RefreshZone;

            RefreshScore(ScoreManager.Instance.CurrentScore);
            RefreshZone(GameManager.Instance.currentZoneName);

            inventoryPanel?.SetActive(false);
            menuPanel?.SetActive(false);
            RefreshSoundButtonLabel();
            SetInteractPrompt(null);
        }

        // ── Help content ────────────────────────────────────────────────────
        private const string HelpContent =
            "<b>CONTROLS</b>\n" +
            "──────────────────────────\n" +
            "<b>WASD / Arrows</b>        Move\n" +
            "<b>Right-Click + Drag</b>   Rotate Camera\n" +
            "<b>E</b>                    Interact\n" +
            "<b>1 / 2 / 3</b>            Select Inventory Slot\n" +
            "<b>I</b>  or  [BAG]         Toggle Inventory\n" +
            "<b>Space / Enter</b>         Close Popup\n" +
            "<b>H</b>                    Toggle This Help";

        private const string AboutContent =
            "<b>WHO WIRED THIS</b>\n" +
            "Puzzle adventure prototype built with Unity.\n\n" +
            "Use the top bar to manage inventory, terminal, and game options.";

        // ── Refresh helpers ──────────────────────────────────────────────────

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

        // ── Public API ───────────────────────────────────────────────────────

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

        public void ToggleHelp()
        {
            if (MessagePanel.Instance == null)
            {
                return;
            }

            if (MessagePanel.Instance.IsVisible)
            {
                MessagePanel.Instance.Hide();
            }
            else
            {
                MessagePanel.Instance.Show(HelpContent);
            }
        }

        public void ToggleMenuPanel()
        {
            ToggleHamburgerMenu();
        }

        private void ToggleHamburgerMenu()
        {
            if (menuPanel != null)
            {
                menuPanel.SetActive(!menuPanel.activeSelf);
            }
        }

        private void ToggleSound()
        {
            soundEnabled = !soundEnabled;
            AudioListener.volume = soundEnabled ? 1f : 0f;
            RefreshSoundButtonLabel();
        }

        private void RequestRestart()
        {
            MessagePanel.Instance?.ShowConfirmation(
                "Restart game?",
                ConfirmRestart);
        }

        private void ConfirmRestart()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.name);
        }

        private void ShowAbout()
        {
            MessagePanel.Instance?.Show(AboutContent);
        }

        private void RefreshSoundButtonLabel()
        {
            if (soundButtonLabel != null)
            {
                soundButtonLabel.text = soundEnabled ? "Sound: On" : "Sound: Off";
            }
        }
    }
}
