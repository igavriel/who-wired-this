using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WhoWiredThis.Core;
using WhoWiredThis.Environment;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WhoWiredThis.UI
{
    public class GameOverSceneController : MonoBehaviour
    {
        [SerializeField] private TMP_Text crewRankLabel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private SceneFlowBootstrapConfig flowBootstrap;
        [SerializeField] private KeyCode playerAActionKey = KeyCode.LeftControl;
        [SerializeField] private KeyCode playerBActionKey = KeyCode.RightControl;

        private bool hasRestarted;

        private void Start()
        {
            ValidateReferences();

            float elapsedSeconds = PlaytestRunSummary.HasSummary
                ? PlaytestRunSummary.Current.RunTimeSeconds
                : ScoreManager.GetTotalSeconds();

            int teamScore = PlaytestTeamScoreCalculator.CalculateTeamScore();
            Debug.Log(
                $"[GameOverSceneController] Final total elapsed time: {elapsedSeconds:F2}s " +
                $"({ScoreManager.FormatTime(elapsedSeconds)}). Team score: {teamScore}.");

            ApplyRunSummaryDisplays();

            if (!PlaytestRunSummary.HasSummary && crewRankLabel != null)
            {
                crewRankLabel.text = $"Score: {teamScore}";
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestartClicked);
                restartButton.onClick.AddListener(HandleRestartClicked);
            }

            if (quitButton != null)
            {
                quitButton.gameObject.SetActive(false);
                quitButton.onClick.RemoveListener(HandleQuitClicked);
            }
        }

        private void Update()
        {
            if (hasRestarted)
            {
                return;
            }

            if (Input.GetKeyDown(playerAActionKey) || Input.GetKeyDown(playerBActionKey))
            {
                Debug.Log("[GameOverSceneController] Player action key pressed. Restarting run.");
                HandleRestartClicked();
            }
        }

        private void ValidateReferences()
        {
            if (crewRankLabel == null)
            {
                Debug.LogWarning("[GameOverSceneController] crewRankLabel is not assigned.", this);
            }

            if (restartButton == null)
            {
                Debug.LogWarning("[GameOverSceneController] restartButton is not assigned.", this);
            }

            if (quitButton == null)
            {
                Debug.LogWarning("[GameOverSceneController] quitButton is not assigned.", this);
            }
        }

        private void HandleRestartClicked()
        {
            if (hasRestarted)
            {
                return;
            }

            hasRestarted = true;
            Debug.Log("[GameOverSceneController] Restart clicked.");

            if (flowBootstrap == null)
            {
                flowBootstrap = SceneFlowBootstrapConfig.FindBootstrap();
            }

            if (flowBootstrap != null)
            {
                if (flowBootstrap.TryLoadSceneById(
                        PlaytestSceneId.StartScene,
                        ignoreWhenAlreadyInTargetScene: true,
                        out string bootstrapError))
                {
                    return;
                }

                Debug.LogWarning($"[GameOverSceneController] Bootstrap restart failed: {bootstrapError}");
            }

            if (!PlaytestFlowUtility.TryReturnToMainMenu(PlaytestFlowUtility.DefaultStartSceneName, out _))
            {
                hasRestarted = false;
            }
        }

        private void HandleQuitClicked()
        {
            Debug.Log("[GameOverSceneController] Quit clicked.");
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void ApplyRunSummaryDisplays()
        {
            if (!PlaytestRunSummary.HasSummary)
            {
                return;
            }

            string summaryText = PlaytestRunSummary.FormatDisplayText();
            TMP_Text[] labels = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
            bool appliedToSummaryLabel = false;

            for (int i = 0; i < labels.Length; i++)
            {
                TMP_Text label = labels[i];
                if (label != null && label.name == "RunSummaryText")
                {
                    ConfigureSummaryLabel(label);
                    label.text = summaryText;
                    appliedToSummaryLabel = true;
                }
            }

            if (appliedToSummaryLabel)
            {
                return;
            }

            for (int i = 0; i < labels.Length; i++)
            {
                TMP_Text label = labels[i];
                if (label != null && label.name == "CrewRankText")
                {
                    ConfigureSummaryLabel(label);
                    label.text = summaryText;
                }
            }
        }

        private static void ConfigureSummaryLabel(TMP_Text label)
        {
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.characterSpacing = 0f;
            label.lineSpacing = 0f;

            TMP_FontAsset monoFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/VT323-Regular SDF");
            if (monoFont == null)
            {
#if UNITY_EDITOR
                monoFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/TextMesh Pro/Fonts/VT323-Regular SDF.asset");
#endif
            }

            if (monoFont != null)
            {
                label.font = monoFont;
            }
        }
    }
}
