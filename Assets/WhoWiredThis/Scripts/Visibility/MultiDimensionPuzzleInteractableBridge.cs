using System.Collections;
using UnityEngine;
using WhoWiredThis.Interfaces;
using WhoWiredThis.PanelFocus;
using WhoWiredThis.Puzzles.Common;
using WhoWiredThis.Util;

namespace WhoWiredThis.Visibility
{
    /// <summary>
    /// Interactable entrypoint that forwards interaction to a MultiDimension puzzle manager.
    /// </summary>
    public class MultiDimensionPuzzleInteractableBridge : MonoBehaviour, IInteractable
    {
        [Header("Target")]
        [Tooltip("Reference must be a MultiDimensionPuzzleManager.")]
        [RequireInterface(typeof(MultiDimensionPuzzleManager))]
        [SerializeField] private MonoBehaviour puzzleTargetReference;

        [Header("Optional feedback")]
        [Tooltip("When set, plays press animation before processing (if any) and TryCheckSolutionFromInteractor.")]
        [SerializeField] private ActivateButtonFeedbackController pressFeedback;

        [Tooltip("When set, runs processing lines on the diagnostic body before TryCheckSolutionFromInteractor.")]
        [SerializeField] private ProcessingFeedbackController processingFeedback;

        [Tooltip("Optional 2-state lever feedback: ON at submit, OFF after delay on failure, latched ON on success.")]
        [SerializeField] private SubmitLeverMultiDimensionFeedback leverFeedback;

        [Tooltip("When locked, Interact is ignored. Leave empty to use a PanelActionLock on a panel ancestor.")]
        [SerializeField] private PanelActionLock panelActionLock;

        [Header("Prompt")]
        [SerializeField] private string interactPrompt = "$INTERACT$ Check combination";
        [SerializeField] private string solvedPrompt = "Combination solved.";

        private bool activateFlowRunning;

        private MultiDimensionPuzzleManager PuzzleTarget => puzzleTargetReference as MultiDimensionPuzzleManager;

        public string GetPromptText()
        {
            MultiDimensionPuzzleManager target = PuzzleTarget;
            return target != null && target.Solved ? solvedPrompt : interactPrompt;
        }

        public void Interact(GameObject interactor)
        {
            if (activateFlowRunning)
            {
                Debug.LogWarning(
                    $"[MultiDimensionPuzzleInteractableBridge] '{name}' ignored Interact while activate flow is running.",
                    this);
                return;
            }

            PanelActionLock gate = PanelActionLock.Resolve(this, panelActionLock);
            if (gate != null && gate.IsLocked)
            {
                return;
            }

            MultiDimensionPuzzleManager target = PuzzleTarget;
            if (target == null)
            {
                Debug.LogWarning($"[MultiDimensionPuzzleInteractableBridge] Missing puzzle target on '{name}'.", this);
                return;
            }

            Debug.Log(
                $"[MultiDimensionPuzzleInteractableBridge] '{name}' starting activate flow for manager '{target.name}'. " +
                $"interactor={(interactor != null ? interactor.name : "null")}.",
                this);
            // Run on the puzzle manager when possible so disabling *this* bridge for Activate does not stop the coroutine.
            // Fallback: ProcessingFeedbackController, then this bridge.
            MonoBehaviour coroutineHost = PuzzleTarget != null ? PuzzleTarget :
                (processingFeedback != null ? processingFeedback : (MonoBehaviour)this);
            coroutineHost.StartCoroutine(RunActivateFlow(interactor));
        }

        private IEnumerator RunActivateFlow(GameObject interactor)
        {
            activateFlowRunning = true;
            try
            {
                leverFeedback?.SetSubmitOn();

                if (pressFeedback != null)
                {
                    yield return pressFeedback.PlayPressFeedbackRoutine();
                }

                if (processingFeedback != null)
                {
                    yield return processingFeedback.PlayProcessingRoutine();
                }

                MultiDimensionPuzzleManager target = PuzzleTarget;
                if (target == null)
                {
                    Debug.LogWarning(
                        $"[MultiDimensionPuzzleInteractableBridge] '{name}' puzzle target missing after processing; skipping check.",
                        this);
                    yield break;
                }

                target.TryCheckSolutionFromInteractor(interactor);

                if (leverFeedback != null)
                {
                    yield return leverFeedback.FinishSubmitRoutine(target.Solved);
                }
            }
            finally
            {
                RestoreActivateIfNeeded();
                activateFlowRunning = false;
            }
        }

        private void RestoreActivateIfNeeded()
        {
            if (processingFeedback == null || processingFeedback.ActivateInteractable == null)
            {
                return;
            }

            MultiDimensionPuzzleManager target = PuzzleTarget;
            if (target != null && target.Solved)
            {
                return;
            }

            processingFeedback.ActivateInteractable.enabled = true;
        }
    }
}
