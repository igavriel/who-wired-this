using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WhoWiredThis.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WhoWiredThis.UI
{
    public class GameOverSceneController : MonoBehaviour
    {
        private const string BestTimeKey = "PlaytestBestTimeSeconds";

        [SerializeField] private TMP_Text completionTimeLabel;
        [SerializeField] private TMP_Text bestTimeLabel;
        [SerializeField] private TMP_Text crewRankLabel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private string startSceneName = "StartScene";
        [SerializeField] private KeyCode playerAActionKey = KeyCode.LeftControl;
        [SerializeField] private KeyCode playerBActionKey = KeyCode.RightControl;
        [SerializeField] private KeyCode bossModifierKey = KeyCode.F12;
        [SerializeField] private KeyCode bossResetKey = KeyCode.Alpha1;

        private bool hasRestarted;

        private void Start()
        {
            ValidateReferences();

            float elapsedSeconds = PlaytestRunTotal.GetTotalSeconds();
            Debug.Log($"[GameOverSceneController] Final total elapsed time: {elapsedSeconds:F2}s ({PlaytestRunTotal.FormatTime(elapsedSeconds)}).");

            float bestSeconds = PlayerPrefs.GetFloat(BestTimeKey, 0f);
            bool hasValidBest = PlayerPrefs.HasKey(BestTimeKey) && bestSeconds > 0f;
            bool hasValidCurrentRun = elapsedSeconds > 0f;

            if (!hasValidBest)
            {
                if (hasValidCurrentRun)
                {
                    bestSeconds = elapsedSeconds;
                    PlayerPrefs.SetFloat(BestTimeKey, bestSeconds);
                    PlayerPrefs.Save();
                    Debug.Log($"[GameOverSceneController] First valid run. Best time initialized to {bestSeconds:F2}s.");
                }
                else
                {
                    Debug.Log("[GameOverSceneController] No valid current run time yet; 00:00 best is ignored.");
                }
            }
            else
            {
                if (hasValidCurrentRun && elapsedSeconds < bestSeconds)
                {
                    bestSeconds = elapsedSeconds;
                    PlayerPrefs.SetFloat(BestTimeKey, bestSeconds);
                    PlayerPrefs.Save();
                    Debug.Log($"[GameOverSceneController] New best time saved: {bestSeconds:F2}s.");
                }
                else
                {
                    Debug.Log($"[GameOverSceneController] Best time not updated. Current best: {bestSeconds:F2}s.");
                }
            }

            if (completionTimeLabel != null)
            {
                completionTimeLabel.text = $"Completion Time: {PlaytestRunTotal.FormatTime(elapsedSeconds)}";
            }

            if (bestTimeLabel != null)
            {
                bestTimeLabel.text = $"Best Time: {PlaytestRunTotal.FormatTime(bestSeconds)}";
            }

            if (crewRankLabel != null)
            {
                crewRankLabel.text = $"Crew Rank: {GetCrewRank(elapsedSeconds)}";
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
            if (IsBossResetPressed())
            {
                ResetBestTime();
            }

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
            if (completionTimeLabel == null)
            {
                Debug.LogWarning("[GameOverSceneController] completionTimeLabel is not assigned.", this);
            }

            if (bestTimeLabel == null)
            {
                Debug.LogWarning("[GameOverSceneController] bestTimeLabel is not assigned.", this);
            }

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

        private bool IsBossResetPressed()
        {
            return (Input.GetKey(bossModifierKey) && Input.GetKeyDown(bossResetKey)) ||
                   (Input.GetKey(bossResetKey) && Input.GetKeyDown(bossModifierKey));
        }

        private void ResetBestTime()
        {
            PlayerPrefs.DeleteKey(BestTimeKey);
            PlayerPrefs.Save();
            Debug.Log("[GameOverSceneController] Boss key pressed. Best time was reset.");

            if (bestTimeLabel != null)
            {
                bestTimeLabel.text = "Best Time: 00:00";
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

            if (!PlaytestFlowUtility.TryReturnToMainMenu(startSceneName, out string loadError))
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

        private static string GetCrewRank(float elapsedSeconds)
        {
            if (elapsedSeconds < 300f)
            {
                return "Expert Repair Crew";
            }

            if (elapsedSeconds < 480f)
            {
                return "Certified Operators";
            }

            if (elapsedSeconds < 720f)
            {
                return "Trainee Technicians";
            }

            return "System Still Concerned";
        }
    }
}
