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
        [SerializeField] private MultiDimensionPuzzelManager puzzleManager;
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

            RefreshDisplay(force: true);
        }

        private void OnDisable()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnAttemptSubmitted -= HandleAttemptSubmitted;
            }
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
            RefreshDisplay(force: true);
        }

        private void RefreshDisplay(bool force)
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

            diagnosticDisplay.SetDiagnosticResult(
                metric1Label, recognized, total,
                metric2Label, aligned, total,
                BuildMessage(recognized, aligned, total));
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
