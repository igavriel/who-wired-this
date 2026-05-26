using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using WhoWiredThis.PanelFocus;
using WhoWiredThis.Puzzles.Common;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Tutorial
{
    public enum TutorialSessionStage
    {
        PlayerAOperator = 0,
        PlayerBOperator = 1,
        Complete = 2
    }

    /// <summary>
    /// Action-area lock only: input / Send colliders plus glass hint. Panel focus and board entry stay driven
    /// by <see cref="WhoWiredThis.PanelFocus.InitialPanelFocusBootstrap"/> and <see cref="WhoWiredThis.PanelFocus.PlayerPanelFocusController"/>; do not exit or disable focus here.
    /// </summary>
    [Serializable]
    public class TutorialPanelLockBundle
    {
        [Header("Player-facing copy (Blue / Red in UI only)")]
        [SerializeField]
        [TextArea(2, 5)]
        private string waitingOverlayText =
            "WAITING FOR YOUR TURN...\nREAD THE DIAGNOSTIC\nTALK TO YOUR PARTNER";

        [Header("Action area only (knob/slider/send, etc.)")]
        [SerializeField]
        [Tooltip("Colliders for input modules and Send; toggled per tutorial stage. Board / panel focus entry is not modified.")]
        private Collider[] actionColliders;

        [Tooltip("Logical gate so keyboard / direct Interact paths cannot bypass disabled colliders.")]
        [SerializeField]
        private PanelActionLock panelActionLock;

        [Header("Glass overlay (visual only; does not block rays)")]
        [SerializeField]
        private GameObject glassOverlayRoot;

        [SerializeField]
        private TMP_Text overlayInstructionText;

        public void ApplyWaitingState()
        {
            panelActionLock?.SetLocked(true);
            SetActionCollidersEnabled(false);

            if (glassOverlayRoot != null)
            {
                glassOverlayRoot.SetActive(true);
            }

            if (overlayInstructionText != null)
            {
                overlayInstructionText.text = waitingOverlayText ?? string.Empty;
                overlayInstructionText.gameObject.SetActive(true);
            }
        }

        public void ApplyOperatorState()
        {
            panelActionLock?.SetLocked(false);
            SetActionCollidersEnabled(true);

            if (glassOverlayRoot != null)
            {
                glassOverlayRoot.SetActive(false);
            }
        }

        /// <summary>Tutorial finished: action colliders off; no completion UI on overlay.</summary>
        public void ApplyCompleteLock()
        {
            panelActionLock?.SetLocked(true);
            SetActionCollidersEnabled(false);

            if (glassOverlayRoot != null)
            {
                glassOverlayRoot.SetActive(false);
            }

            if (overlayInstructionText != null)
            {
                overlayInstructionText.gameObject.SetActive(false);
            }
        }

        private void SetActionCollidersEnabled(bool enabled)
        {
            if (actionColliders == null)
            {
                return;
            }

            for (int i = 0; i < actionColliders.Length; i++)
            {
                SetColliderEnabled(actionColliders[i], enabled);
            }
        }

        private static void SetColliderEnabled(Collider collider, bool enabled)
        {
            if (collider != null)
            {
                collider.enabled = enabled;
            }
        }
    }

    /// <summary>
    /// Staged tutorial flow on top of two MultiDimensionPuzzleManager instances. Does not change puzzle logic.
    /// Runs after InitialPanelFocusBootstrap via DefaultExecutionOrder.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class TutorialStageManager : MonoBehaviour
    {
        private const string LogPrefix = "[TutorialStageManager]";

        [Header("Blue / Red = UI labels only — maps to Player A / B")]
        [SerializeField]
        private MultiDimensionPuzzleManager playerAPuzzleManager;

        [SerializeField]
        private MultiDimensionPuzzleManager playerBPuzzleManager;

        [SerializeField]
        private TutorialPanelLockBundle playerAPanelLock;

        [SerializeField]
        private TutorialPanelLockBundle playerBPanelLock;

        [Header("Diagnostic Body_TMP (tutorial copy only at stage boundaries)")]
        [SerializeField]
        private DiagnosticDisplayController playerADiagnosticDisplay;

        [SerializeField]
        private DiagnosticDisplayController playerBDiagnosticDisplay;

        [SerializeField]
        [TextArea(3, 8)]
        private string playerAIntroBody =
            "SET TWO CONTROLS.\n" +
            "PRESS SEND.\n" +
            "YOUR PARTNER READS THE MACHINE.";

        [SerializeField]
        [TextArea(3, 8)]
        private string playerBIntroBody =
            "READ THIS PANEL.\n" +
            "EXPLAIN WHAT THE MACHINE UNDERSTOOD.\n" +
            "USE THE HISTORY.";

        [SerializeField]
        [TextArea(2, 6)]
        private string playerABodyAfterPlayerASolved =
            "BLUE SIDE CALIBRATED.\n" +
            "NOW READ THE DIAGNOSTIC.";

        [SerializeField]
        [TextArea(2, 6)]
        private string playerBBodyAfterPlayerASolved =
            "YOUR TURN.\n" +
            "SET TWO CONTROLS.\n" +
            "PRESS SEND.";

        [Header("Completion hook (no UI in this task)")]
        [SerializeField]
        private GameObject[] objectsToDisableOnComplete;

        [SerializeField]
        private Collider[] exitDoorBlockersToDisableOnComplete;

        [SerializeField]
        [TextArea(5, 12)]
        private string completionMessage =
            "Synchronization confirmed.\n" +
            "The exit door is now unlocked.\n\n" +
            "Double-click the Exit button to close this panel and return to the game.";

        private TutorialSessionStage stage = TutorialSessionStage.PlayerAOperator;
        private bool completionRaised;

        public TutorialSessionStage CurrentStage => stage;

        /// <summary>Raised once after initial <see cref="TutorialSessionStage.PlayerAOperator"/> locks are applied and input is allowed for the operator.</summary>
        public event Action OnTutorialStarted;

        public event Action<TutorialSessionStage> OnStageChanged;

        /// <summary>Raised once when both panels are solved and the tutorial locks both sides.</summary>
        public event Action OnTutorialCompleted;

        private void OnEnable()
        {
            if (playerAPuzzleManager != null)
            {
                playerAPuzzleManager.OnAttemptSubmitted += HandlePlayerAAttempt;
            }
            else
            {
                Debug.LogWarning($"{LogPrefix} playerAPuzzleManager is not assigned on '{name}'.", this);
            }

            if (playerBPuzzleManager != null)
            {
                playerBPuzzleManager.OnAttemptSubmitted += HandlePlayerBAttempt;
            }
            else
            {
                Debug.LogWarning($"{LogPrefix} playerBPuzzleManager is not assigned on '{name}'.", this);
            }
        }

        private void OnDisable()
        {
            if (playerAPuzzleManager != null)
            {
                playerAPuzzleManager.OnAttemptSubmitted -= HandlePlayerAAttempt;
            }

            if (playerBPuzzleManager != null)
            {
                playerBPuzzleManager.OnAttemptSubmitted -= HandlePlayerBAttempt;
            }
        }

        private void Start()
        {
            ApplyStageVisualAndLocks();
            OnTutorialStarted?.Invoke();
            NotifyStageChanged();
            StartCoroutine(BootstrapTutorialDiagnosticCopy());
        }

        private void NotifyStageChanged()
        {
            OnStageChanged?.Invoke(stage);
        }

        private IEnumerator BootstrapTutorialDiagnosticCopy()
        {
            yield return null;
            ApplyIntroDiagnosticBodies();
        }

        private void ApplyIntroDiagnosticBodies()
        {
            TrySetInstructionBody(playerADiagnosticDisplay, playerAIntroBody, "playerADiagnosticDisplay is not assigned; skipping intro body copy.");
            TrySetInstructionBody(playerBDiagnosticDisplay, playerBIntroBody, "playerBDiagnosticDisplay is not assigned; skipping intro body copy.");
        }

        private IEnumerator ApplyRoleSwitchBodiesAfterDelay()
        {
            yield return null;
            TrySetInstructionBody(
                playerADiagnosticDisplay,
                playerABodyAfterPlayerASolved,
                "playerADiagnosticDisplay is not assigned; skipping post-solve body copy.");
            TrySetInstructionBody(
                playerBDiagnosticDisplay,
                playerBBodyAfterPlayerASolved,
                "playerBDiagnosticDisplay is not assigned; skipping post-solve body copy.");
        }

        private void TrySetInstructionBody(DiagnosticDisplayController display, string body, string nullWarningDetail)
        {
            if (display == null)
            {
                Debug.LogWarning($"{LogPrefix} {nullWarningDetail}", this);
                return;
            }

            display.SetInstructionBody(body ?? string.Empty);
        }

        private void HandlePlayerAAttempt(MultiDimensionAttemptResult result)
        {
            if (result == null || !result.IsSolved)
            {
                return;
            }

            if (stage != TutorialSessionStage.PlayerAOperator)
            {
                return;
            }

            stage = TutorialSessionStage.PlayerBOperator;
            ApplyStageVisualAndLocks();
            NotifyStageChanged();
            StartCoroutine(ApplyRoleSwitchBodiesAfterDelay());
        }

        private void HandlePlayerBAttempt(MultiDimensionAttemptResult result)
        {
            if (result == null || !result.IsSolved)
            {
                return;
            }

            if (stage != TutorialSessionStage.PlayerBOperator)
            {
                return;
            }

            stage = TutorialSessionStage.Complete;
            ApplyStageVisualAndLocks();
            NotifyStageChanged();
            RaiseCompletionOnce();
        }

        private void RaiseCompletionOnce()
        {
            if (completionRaised)
            {
                return;
            }

            completionRaised = true;
            DisableConfiguredObjectsOnComplete();
            DisableExitDoorBlockersOnComplete();
            ShowCompletionMessageOnDiagnostics();
            OnTutorialCompleted?.Invoke();
        }

        private void DisableConfiguredObjectsOnComplete()
        {
            if (objectsToDisableOnComplete == null || objectsToDisableOnComplete.Length == 0)
            {
                return;
            }

            for (int i = 0; i < objectsToDisableOnComplete.Length; i++)
            {
                GameObject target = objectsToDisableOnComplete[i];
                if (target == null)
                {
                    continue;
                }

                target.SetActive(false);
            }
        }

        private void DisableExitDoorBlockersOnComplete()
        {
            if (exitDoorBlockersToDisableOnComplete == null || exitDoorBlockersToDisableOnComplete.Length == 0)
            {
                return;
            }

            for (int i = 0; i < exitDoorBlockersToDisableOnComplete.Length; i++)
            {
                Collider blocker = exitDoorBlockersToDisableOnComplete[i];
                if (blocker == null)
                {
                    continue;
                }

                blocker.enabled = false;
            }
        }

        private void ShowCompletionMessageOnDiagnostics()
        {
            TrySetCompletionMessage(playerADiagnosticDisplay, "playerADiagnosticDisplay is not assigned; skipping completion copy.");
            TrySetCompletionMessage(playerBDiagnosticDisplay, "playerBDiagnosticDisplay is not assigned; skipping completion copy.");
        }

        private void TrySetCompletionMessage(DiagnosticDisplayController display, string nullWarningDetail)
        {
            if (display == null)
            {
                Debug.LogWarning($"{LogPrefix} {nullWarningDetail}", this);
                return;
            }

            display.SetSuccess(completionMessage ?? string.Empty);
        }

        private void ApplyStageVisualAndLocks()
        {
            if (playerAPanelLock == null || playerBPanelLock == null)
            {
                Debug.LogWarning($"{LogPrefix} Missing playerAPanelLock or playerBPanelLock on '{name}'.", this);
                return;
            }

            switch (stage)
            {
                case TutorialSessionStage.PlayerAOperator:
                    playerAPanelLock.ApplyOperatorState();
                    playerBPanelLock.ApplyWaitingState();
                    break;
                case TutorialSessionStage.PlayerBOperator:
                    playerAPanelLock.ApplyWaitingState();
                    playerBPanelLock.ApplyOperatorState();
                    break;
                case TutorialSessionStage.Complete:
                    playerAPanelLock.ApplyCompleteLock();
                    playerBPanelLock.ApplyCompleteLock();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
