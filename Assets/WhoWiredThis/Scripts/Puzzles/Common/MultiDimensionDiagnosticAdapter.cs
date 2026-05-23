using UnityEngine;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Puzzles.Common
{
    /// <summary>
    /// Bridges MultiDimension puzzle diagnostics into a world-space DiagnosticDisplayController.
    /// Keeps display render-only by doing data collection/formatting here.
    /// </summary>
    public class MultiDimensionDiagnosticAdapter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MultiDimensionPuzzleManager puzzleManager;
        [SerializeField] private DiagnosticDisplayController diagnosticDisplay;

        [Header("Labels")]
        [SerializeField] private string metric1Label = "RECOGNIZED";
        [SerializeField] private string metric2Label = "ALIGNED";

        [Header("Messages")]
        [SerializeField] private string solvedMessage = "A-SIDE CALIBRATED";
        [SerializeField] private string perfectButMisalignedMessage = "CORRECT SIGNALS,\nWRONG ORDER.";
        [SerializeField] private string noMatchMessage = "NO MATCHING SIGNALS.\nTRY AGAIN.";
        [SerializeField] private string partialMessage = "PARTIAL MATCH.\nKEEP ADJUSTING.";

        [Header("Behavior")]
        [Tooltip("When false, diagnostic stays on waiting until a solve attempt; updates only from OnAttemptSubmitted (no live preview while adjusting).")]
        [SerializeField] private bool updateContinuously = true;

        private int lastRecognized = -1;
        private int lastAligned = -1;
        private int lastTotal = -1;
        private bool lastSolved;

        private void OnEnable()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnAttemptSubmitted += HandleAttemptSubmitted;
            }

            if (updateContinuously)
            {
                RefreshDisplay(force: true);
                return;
            }

            // Commit-only: show waiting until the player submits a solve; solved state still shows on load.
            if (diagnosticDisplay == null)
            {
                return;
            }

            if (puzzleManager != null && puzzleManager.Solved)
            {
                RefreshDisplay(force: true);
            }
            else
            {
                lastRecognized = -1;
                lastAligned = -1;
                lastTotal = -1;
                lastSolved = false;
                diagnosticDisplay.SetWaiting();
            }
        }

        private void OnDisable()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnAttemptSubmitted -= HandleAttemptSubmitted;
            }
        }

        private void Start()
        {
            if (updateContinuously || diagnosticDisplay == null || puzzleManager == null || puzzleManager.Solved)
            {
                return;
            }

            lastRecognized = -1;
            lastAligned = -1;
            lastTotal = -1;
            lastSolved = false;
            diagnosticDisplay.SetWaiting();
        }

        private void Update()
        {
            if (!updateContinuously)
            {
                return;
            }

            RefreshDisplay(force: false);
        }

        private void HandleAttemptSubmitted(MultiDimensionAttemptResult _)
        {
            RefreshDisplay(force: true, appendUnsolvedFlavorFooter: true);
        }

        private void RefreshDisplay(bool force, bool appendUnsolvedFlavorFooter = false)
        {
            if (puzzleManager == null || diagnosticDisplay == null)
            {
                return;
            }

            if (!puzzleManager.TryGetDiagnosticSnapshot(out int recognized, out int aligned, out int total))
            {
                diagnosticDisplay.SetWaiting();
                return;
            }

            bool solved = puzzleManager.Solved;
            if (!force &&
                recognized == lastRecognized &&
                aligned == lastAligned &&
                total == lastTotal &&
                solved == lastSolved)
            {
                return;
            }

            lastRecognized = recognized;
            lastAligned = aligned;
            lastTotal = total;
            lastSolved = solved;

            if (solved)
            {
                diagnosticDisplay.SetSuccess(solvedMessage);
                return;
            }

            string clue = BuildMessage(recognized, aligned, total);
            string flavor = null;
            if (appendUnsolvedFlavorFooter)
            {
                MachineFeedbackTextController machine = diagnosticDisplay.GetMachineFeedbackText();
                if (machine != null)
                {
                    flavor = machine.GetRandomFlavorLine();
                }
            }

            diagnosticDisplay.SetDiagnosticResult(
                metric1Label, recognized, total,
                metric2Label, aligned, total,
                clue,
                flavor);
        }

        private string BuildMessage(int recognized, int aligned, int total)
        {
            if (recognized >= total && aligned < total)
            {
                return perfectButMisalignedMessage;
            }

            if (recognized == 0)
            {
                return noMatchMessage;
            }

            return partialMessage;
        }
    }
}
