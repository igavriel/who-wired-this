using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using WhoWiredThis.Scenes;
using WhoWiredThis.UI;

namespace WhoWiredThis.Tutorial
{
    /// <summary>
    /// Tutorial-only: shows a team summary on both player HUDs when the tutorial completes.
    /// May later be generalized for other puzzle runs; keep formatting isolated in <see cref="BuildSummaryText"/>.
    /// </summary>
    public class TutorialSummaryPopupPresenter : MonoBehaviour
    {
        private const string LogPrefix = "[TutorialSummaryPopupPresenter]";

        [Header("References")]
        [FormerlySerializedAs("tutorialStageManager")]
        [SerializeField]
        private SceneStageManager sceneStageManager;

        [SerializeField]
        private TutorialMetricsTracker tutorialMetricsTracker;

        [SerializeField]
        private PlayerHudView playerHudViewA;

        [SerializeField]
        private PlayerHudView playerHudViewB;

        [Header("Copy")]
        [SerializeField]
        private string headerLine = "CALIBRATION COMPLETE";

        [SerializeField]
        private string teamAttemptsLabel = "TEAM ATTEMPTS";

        [SerializeField]
        private string teamTimeLabel = "TEAM TIME";

        [SerializeField]
        private string playerALabel = "PLAYER A";

        [SerializeField]
        private string playerBLabel = "PLAYER B";

        [SerializeField]
        private string attemptsSuffix = "ATTEMPTS";

        [SerializeField]
        private string flavorLine = "THE MACHINE SURVIVED.";

        [SerializeField]
        private bool includeFlavorLine = true;

        private void OnEnable()
        {
            if (sceneStageManager != null)
            {
                sceneStageManager.OnStageCompleted += HandleStageCompleted;
            }
            else
            {
                Debug.LogWarning($"{LogPrefix} sceneStageManager is not assigned.", this);
            }
        }

        private void OnDisable()
        {
            if (sceneStageManager != null)
            {
                sceneStageManager.OnStageCompleted -= HandleStageCompleted;
            }
        }

        private void HandleStageCompleted()
        {
            StartCoroutine(ShowSummaryNextFrame());
        }

        private IEnumerator ShowSummaryNextFrame()
        {
            yield return null;

            if (tutorialMetricsTracker == null)
            {
                Debug.LogWarning($"{LogPrefix} tutorialMetricsTracker is not assigned.", this);
                yield break;
            }

            TutorialMetricsSnapshot snapshot = tutorialMetricsTracker.GetSnapshot();
            if (!snapshot.TutorialComplete)
            {
                Debug.LogWarning($"{LogPrefix} Tutorial not complete in metrics snapshot; skipping summary.", this);
                yield break;
            }

            string summaryText = BuildSummaryText(snapshot);
            ShowOnHud(playerHudViewA, "playerHudViewA", summaryText);
            ShowOnHud(playerHudViewB, "playerHudViewB", summaryText);
        }

        private void ShowOnHud(PlayerHudView hud, string fieldName, string summaryText)
        {
            if (hud == null)
            {
                Debug.LogWarning($"{LogPrefix} {fieldName} is not assigned.", this);
                return;
            }

            hud.ShowPopup(summaryText);
        }

        private string BuildSummaryText(TutorialMetricsSnapshot snapshot)
        {
            return BuildSummaryText(
                snapshot,
                headerLine,
                teamAttemptsLabel,
                teamTimeLabel,
                playerALabel,
                playerBLabel,
                attemptsSuffix,
                flavorLine,
                includeFlavorLine);
        }

        internal static string BuildSummaryText(
            TutorialMetricsSnapshot snapshot,
            string header,
            string teamAttemptsLabel,
            string teamTimeLabel,
            string playerALabel,
            string playerBLabel,
            string attemptsSuffix,
            string flavorLine,
            bool includeFlavorLine)
        {
            var builder = new StringBuilder();
            builder.AppendLine(header);
            builder.AppendLine();
            builder.AppendLine($"{teamAttemptsLabel}: {snapshot.TotalAttempts}");
            builder.AppendLine($"{teamTimeLabel}: {FormatMmSs(snapshot.TotalElapsedSeconds)}");
            builder.AppendLine();
            builder.AppendLine($"{playerALabel}: {snapshot.PlayerAAttempts} {attemptsSuffix}");
            builder.AppendLine($"{playerBLabel}: {snapshot.PlayerBAttempts} {attemptsSuffix}");

            if (includeFlavorLine && !string.IsNullOrEmpty(flavorLine))
            {
                builder.AppendLine();
                builder.Append(flavorLine);
            }

            return builder.ToString().TrimEnd();
        }

        internal static string FormatMmSs(float elapsedSeconds)
        {
            int totalSec = Mathf.Max(0, Mathf.FloorToInt(elapsedSeconds));
            int minutes = totalSec / 60;
            int seconds = totalSec % 60;
            return $"{minutes:00}:{seconds:00}";
        }
    }
}
