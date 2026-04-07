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
        // Assigned via Inspector or auto-created at runtime if null.
        public GameObject helpPanel;

        private bool inventoryVisible;
        private bool helpVisible;

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

            if (helpPanel == null)
            {
                helpPanel = BuildHelpPanel(); // also wires close button
            }
            else
            {
                WireHelpPanelCloseButton(helpPanel); // pre-assigned via SetupHelpPanel tool
            }

            inventoryPanel?.SetActive(false);
            helpPanel?.SetActive(false);
            SetInteractPrompt(null);
        }

        // ── Help panel ──────────────────────────────────────────────────────

        private void WireHelpPanelCloseButton(GameObject panel)
        {
            Button closeBtn = panel.transform.Find("CloseButton")?.GetComponent<Button>();
            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(ToggleHelp);
            }
        }

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

        /// <summary>
        /// Builds the HelpPanel UGUI hierarchy at runtime and parents it to
        /// the same Canvas this component lives on.
        /// </summary>
        private GameObject BuildHelpPanel()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            if (canvas == null)
            {
                Debug.LogWarning("[WWI] HUDController: no Canvas found — help panel not created.");
                return null;
            }

            // Root panel
            GameObject panel = new GameObject("HelpPanel");
            panel.transform.SetParent(canvas.transform, false);

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.05f, 0.08f, 0.95f);

            RectTransform panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.22f, 0.22f);
            panelRT.anchorMax = new Vector2(0.78f, 0.80f);
            panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;

            // Help text
            GameObject textGO = new GameObject("HelpText");
            textGO.transform.SetParent(panel.transform, false);

            TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = HelpContent;
            tmp.fontSize = 17;
            tmp.color = new Color(0.92f, 0.92f, 0.88f, 1f);
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            tmp.richText = true;
            tmp.margin = new Vector4(20f, 16f, 20f, 52f);

            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = textRT.offsetMax = Vector2.zero;

            // Close button
            GameObject btnGO = new GameObject("CloseButton");
            btnGO.transform.SetParent(panel.transform, false);

            Image btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.15f, 0.15f, 0.18f, 0.9f);

            Button btn = btnGO.AddComponent<Button>();
            btn.onClick.AddListener(ToggleHelp);

            RectTransform btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.28f, 0.02f);
            btnRT.anchorMax = new Vector2(0.72f, 0.18f);
            btnRT.offsetMin = new Vector2(0f, 4f);
            btnRT.offsetMax = new Vector2(0f, -4f);

            GameObject lblGO = new GameObject("Label");
            lblGO.transform.SetParent(btnGO.transform, false);

            TextMeshProUGUI lbl = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.text = "[ H ]  Close Help";
            lbl.fontSize = 15;
            lbl.color = new Color(0.65f, 0.90f, 0.65f, 1f);
            lbl.alignment = TextAlignmentOptions.Midline;

            RectTransform lblRT = lblGO.GetComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;

            return panel;
        }

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
            helpVisible = !helpVisible;
            helpPanel?.SetActive(helpVisible);
        }
    }
}
