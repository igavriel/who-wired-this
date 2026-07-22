using UnityEngine;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Puzzles.Common
{
    /// <summary>
    /// Signal puzzle diagnostic: on each Submit, writes a 40x12 log to the partner's monitor with
    /// distance feedback for rate/power and an ASCII drawing of the TARGET waveform (never its name).
    /// Standby (waiting) before the first Submit; success copy once solved. Replaces the generic
    /// <see cref="ComponentDiagnosticAdapter"/> on Signal panels only — same subscription lifecycle.
    /// </summary>
    public class SignalDiagnosticAdapter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MultiDimensionPuzzleManager puzzleManager;

        [Tooltip("Partner-facing diagnostic monitor (cross-panel, like ComponentDiagnosticAdapter on Signal).")]
        [SerializeField] private DiagnosticDisplayController diagnosticDisplay;

        [Header("Inputs (operator controls on this panel)")]
        [Tooltip("Ordered MIN..MAX control evaluated as SIGNAL RATE (FREQ / TUNE).")]
        [SerializeField] private MultiDimension rateInput;

        [Tooltip("Ordered MIN..MAX control evaluated as SIGNAL POWER (GAIN / AMP).")]
        [SerializeField] private MultiDimension powerInput;

        [Tooltip("Categorical waveform control (WAVE / MODE). Target index selects the ASCII drawing.")]
        [SerializeField] private MultiDimension waveformInput;

        [Header("Copy")]
        [SerializeField] private string headerLine1 = "OTHER PLAYER SUBMITS // YOU READ";
        [SerializeField] private string headerLine2 = "### MATCH THE TARGET SIGNAL ###";
        [SerializeField] private string logTitlePrefix = "SIGNAL LOG // REVISION";
        [SerializeField] private string statusLabel = "STATUS";
        [SerializeField] private string statusValue = "ANALYZING";
        [SerializeField] private string rateLabel = "SIGNAL RATE";
        [SerializeField] private string powerLabel = "SIGNAL POWER";
        [SerializeField] private string waveformLabel = "WAVEFORM MATCH";
        [SerializeField] private string footerLine = "TELL YOUR PARTNER WHAT YOU SEE";
        [SerializeField] private string solvedMessage = "SIGNAL LINK CALIBRATED.";

        [Header("Layout")]
        [SerializeField] private int lineWidth = ComponentDiagnosticLogFormatter.DefaultWidth;
        [SerializeField] private int totalLines = ComponentDiagnosticLogFormatter.DefaultTotalLines;

        private int attemptCounter;

        private void OnEnable()
        {
            if (puzzleManager != null)
            {
                puzzleManager.OnAttemptSubmitted += HandleAttemptSubmitted;
            }

            if (diagnosticDisplay == null)
            {
                return;
            }

            if (puzzleManager != null && puzzleManager.Solved)
            {
                diagnosticDisplay.SetSuccess(solvedMessage);
            }
            else
            {
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
            if (diagnosticDisplay == null || puzzleManager == null || puzzleManager.Solved)
            {
                return;
            }

            diagnosticDisplay.SetWaiting();
        }

        private void HandleAttemptSubmitted(MultiDimensionAttemptResult result)
        {
            if (puzzleManager == null || diagnosticDisplay == null || result == null)
            {
                return;
            }

            if (result.IsSolved || puzzleManager.Solved)
            {
                diagnosticDisplay.SetSuccess(solvedMessage);
                return;
            }

            attemptCounter++;
            string body = BuildFailedAttemptBody(result);
            if (body != null)
            {
                diagnosticDisplay.SetDiagnosticBody(body);
            }
        }

        private string BuildFailedAttemptBody(MultiDimensionAttemptResult result)
        {
            if (!TryEvaluateSlot(rateInput, result, out ComponentSlotDiagnosticStatus rateStatus, out _, "rate") ||
                !TryEvaluateSlot(powerInput, result, out ComponentSlotDiagnosticStatus powerStatus, out _, "power") ||
                !TryEvaluateSlot(waveformInput, result, out ComponentSlotDiagnosticStatus waveStatus, out int waveTargetIndex, "waveform"))
            {
                return null;
            }

            return SignalDiagnosticFormatter.BuildSignalDiagnostic(
                rateStatus,
                powerStatus,
                waveStatus == ComponentSlotDiagnosticStatus.Correct,
                waveTargetIndex,
                attemptCounter,
                headerLine1,
                headerLine2,
                logTitlePrefix,
                statusLabel,
                statusValue,
                rateLabel,
                powerLabel,
                waveformLabel,
                footerLine,
                lineWidth,
                totalLines);
        }

        private bool TryEvaluateSlot(
            MultiDimension input,
            MultiDimensionAttemptResult result,
            out ComponentSlotDiagnosticStatus status,
            out int correctIndex,
            string slotName)
        {
            status = ComponentSlotDiagnosticStatus.Mismatch;
            correctIndex = -1;

            if (input == null)
            {
                Debug.LogWarning($"[{nameof(SignalDiagnosticAdapter)}] '{name}' missing {slotName} input reference.", this);
                return false;
            }

            if (!TryResolveSlotIndex(input, out int slotIndex))
            {
                Debug.LogWarning($"[{nameof(SignalDiagnosticAdapter)}] '{name}' could not resolve puzzle slot for {slotName} input '{input.name}'.", this);
                return false;
            }

            if (result.SubmittedIndices == null ||
                slotIndex < 0 ||
                slotIndex >= result.SubmittedIndices.Length ||
                !puzzleManager.TryGetPuzzleElement(slotIndex, out _, out correctIndex))
            {
                return false;
            }

            int submitted = result.SubmittedIndices[slotIndex];
            status = ComponentDiagnosticClassifier.Classify(ComponentDiagnosticType.Ordered, submitted, correctIndex);
            return true;
        }

        private bool TryResolveSlotIndex(MultiDimension input, out int slotIndex)
        {
            slotIndex = -1;
            if (puzzleManager == null || input == null)
            {
                return false;
            }

            int count = puzzleManager.PuzzleElementCount;
            for (int i = 0; i < count; i++)
            {
                if (puzzleManager.TryGetPuzzleElement(i, out MultiDimension element, out _) &&
                    element == input)
                {
                    slotIndex = i;
                    return true;
                }
            }

            return false;
        }
    }
}
