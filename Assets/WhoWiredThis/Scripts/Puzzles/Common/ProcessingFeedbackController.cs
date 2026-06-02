using System.Collections;
using UnityEngine;

namespace WhoWiredThis.Puzzles.Common
{
    /// <summary>
    /// Scene-authored processing lines shown before a puzzle check runs. Does not create runtime UI.
    /// Prefers <see cref="MachineFeedbackTextController"/> on the same diagnostic panel as <see cref="DiagnosticDisplayController"/>;
    /// otherwise writes single-line steps via <see cref="DiagnosticDisplayController.SetProcessingBodyText"/>.
    /// </summary>
    public class ProcessingFeedbackController : MonoBehaviour
    {
        private static readonly string[] DefaultMessages =
        {
            "READING SIGNAL...",
            "CHECKING SETTINGS...",
            "UPDATING HISTORY..."
        };

        [Header("Diagnostic")]
        [Tooltip("Same DiagnosticDisplayController the MultiDimensionDiagnosticAdapter drives for this activate flow (may be on the other player panel in split layouts).")]
        [SerializeField]
        private DiagnosticDisplayController diagnosticDisplay;

        [Header("Copy")]
        [SerializeField]
        private string[] processingMessages;

        [Min(0.05f)]
        [SerializeField]
        private float timePerMessage = 0.45f;

        [Tooltip("Extra pause after the last processing line, before the puzzle check runs (realtime seconds).")]
        [Min(0f)]
        [SerializeField]
        private float delayAfterLastLine;

        [Header("Activate button")]
        [Tooltip("Typically the Solve / Activate MonoBehaviour (IInteractable) for this panel.")]
        [SerializeField]
        private MonoBehaviour activateInteractable;

        private bool routineRunning;

        /// <summary>Used by <see cref="WhoWiredThis.Visibility.MultiDimensionPuzzleInteractableBridge"/> to re-enable Activate after the check when the puzzle is not solved.</summary>
        public MonoBehaviour ActivateInteractable => activateInteractable;

        public IEnumerator PlayProcessingRoutine()
        {
            if (routineRunning)
            {
                Debug.LogWarning($"[ProcessingFeedbackController] '{name}' ignored nested PlayProcessingRoutine.", this);
                yield break;
            }

            ResolveDiagnosticDisplayIfMissing();

            if (diagnosticDisplay == null)
            {
                Debug.LogWarning(
                    $"[ProcessingFeedbackController] '{name}' missing diagnosticDisplay; skipping processing UI.",
                    this);
                yield break;
            }

            routineRunning = true;
            diagnosticDisplay.BeginBodyWriteSuppress();
            try
            {
                if (activateInteractable != null)
                {
                    activateInteractable.enabled = false;
                }

                MachineFeedbackTextController machine = diagnosticDisplay.GetMachineFeedbackText();
                if (machine != null && machine.CanPlayBodyProcessingFeedback())
                {
                    yield return machine.PlayBodyProcessingFeedback();
                }
                else
                {
                    if (machine != null && !machine.CanPlayBodyProcessingFeedback())
                    {
                        Debug.LogWarning(
                            $"[ProcessingFeedbackController] '{name}' machine feedback cannot play (missing steps/body); using legacy processing lines.",
                            this);
                    }

                    string[] lines = ResolveMessages();
                    if (lines.Length == 0)
                    {
                        Debug.LogWarning($"[ProcessingFeedbackController] '{name}' has no processing messages; skipping.", this);
                    }
                    else
                    {
                        float wait = Mathf.Max(0.05f, timePerMessage);
                        for (int i = 0; i < lines.Length; i++)
                        {
                            diagnosticDisplay.SetProcessingBodyText(lines[i]);
                            yield return new WaitForSecondsRealtime(wait);
                        }
                    }
                }

                if (delayAfterLastLine > 0f)
                {
                    yield return new WaitForSecondsRealtime(delayAfterLastLine);
                }
            }
            finally
            {
                diagnosticDisplay.EndBodyWriteSuppress();
                routineRunning = false;
            }
        }

        private string[] ResolveMessages()
        {
            if (processingMessages != null && processingMessages.Length > 0)
            {
                return processingMessages;
            }

            return DefaultMessages;
        }

        private void ResolveDiagnosticDisplayIfMissing()
        {
            if (diagnosticDisplay != null)
            {
                return;
            }

            diagnosticDisplay = GetComponentInChildren<DiagnosticDisplayController>(true);
        }
    }
}
