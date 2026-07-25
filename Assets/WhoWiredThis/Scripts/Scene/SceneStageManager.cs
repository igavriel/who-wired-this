using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using WhoWiredThis.Enums;
using WhoWiredThis.PanelFocus;
using WhoWiredThis.Puzzles.Common;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Scenes
{
    public enum SceneSessionStage
    {
        PlayerAOperator = 0,
        PlayerBOperator = 1,
        Complete = 2
    }

    public enum SceneRoleSwapMode
    {
        /// <summary>Default: Player A -> Player B operator hand-off happens within this scene.</summary>
        InScene = 0,

        /// <summary>Player A solving raises <see cref="SceneStageManager.OnPhaseOneSolved"/> for a cut-scene
        /// round trip; the starting stage is read from <see cref="SceneRoleState"/> on load.</summary>
        CutSceneRoundTrip = 1
    }

    /// <summary>
    /// Action-area lock only: input / Send colliders plus glass hint. Panel focus and board entry stay driven
    /// by <see cref="InitialPanelFocusBootstrap"/> and <see cref="PlayerPanelFocusController"/>; do not exit or disable focus here.
    /// </summary>
    [Serializable]
    public class ScenePanelLockBundle
    {
        [Header("Player-facing copy (Blue / Red in UI only)")]
        [SerializeField]
        [TextArea(2, 5)]
        private string waitingOverlayText =
            "WAITING FOR YOUR TURN...\nREAD THE DIAGNOSTIC\nTALK TO YOUR PARTNER";

        [Header("Action area only (knob/slider/send, etc.)")]
        [SerializeField]
        [Tooltip("Colliders for input modules and Send; toggled per stage. Board / panel focus entry is not modified.")]
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

        /// <summary>Stage finished: action colliders off; no completion UI on overlay.</summary>
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
    /// Staged two-player flow on top of two MultiDimensionPuzzleManager instances. Does not change puzzle logic.
    /// Runs after InitialPanelFocusBootstrap via DefaultExecutionOrder.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class SceneStageManager : MonoBehaviour
    {
        private const string LogPrefix = "[SceneStageManager]";

        [Header("Blue / Red = UI labels only — maps to Player A / B")]
        [SerializeField]
        private MultiDimensionPuzzleManager playerAPuzzleManager;

        [SerializeField]
        private MultiDimensionPuzzleManager playerBPuzzleManager;

        [SerializeField]
        private ScenePanelLockBundle playerAPanelLock;

        [SerializeField]
        private ScenePanelLockBundle playerBPanelLock;

        [Header("Diagnostic Body_TMP (stage copy at boundaries)")]
        [SerializeField]
        [Tooltip("When enabled, intro and post-solve bodies use PanelOperationGuideFormatter instead of the TextArea fields below.")]
        private bool useFormattedOperationGuides = true;

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

        [Header("Playtest puzzles")]
        [SerializeField]
        [Tooltip("When enabled, both players can operate their panels at the same time. Completion fires when both puzzle managers are solved.")]
        private bool simultaneousOperators;

        [Header("Role swap")]
        [SerializeField]
        [Tooltip("InScene = Player A -> Player B operator switch within this scene (default, unchanged). " +
            "CutSceneRoundTrip = Player A solving raises OnPhaseOneSolved for a cut-scene round trip; " +
            "the starting stage is read from SceneRoleState on load. Ignored when simultaneousOperators is on.")]
        private SceneRoleSwapMode roleSwapMode = SceneRoleSwapMode.InScene;

        private SceneSessionStage stage = SceneSessionStage.PlayerAOperator;
        private bool completionRaised;
        private bool phaseOneSolvedRaised;

        public SceneSessionStage CurrentStage => stage;

        /// <summary>Raised once after initial <see cref="SceneSessionStage.PlayerAOperator"/> locks are applied and input is allowed for the operator.</summary>
        public event Action OnStageStarted;

        public event Action<SceneSessionStage> OnStageChanged;

        /// <summary>Raised once when both panels are solved and the stage locks both sides.</summary>
        public event Action OnStageCompleted;

        /// <summary>Cut-scene mode only: raised once when the Phase-1 operator (Player A) solves, so a listener
        /// can run the role-swap cut-scene round trip instead of switching operators within this scene.</summary>
        public event Action OnPhaseOneSolved;

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
            if (roleSwapMode == SceneRoleSwapMode.CutSceneRoundTrip && !simultaneousOperators)
            {
                stage = SceneRoleState.HasSwapped
                    ? SceneSessionStage.PlayerBOperator
                    : SceneSessionStage.PlayerAOperator;
            }

            if (simultaneousOperators)
            {
                ApplySimultaneousOperatorLocks();
            }
            else
            {
                ApplyStageVisualAndLocks();
            }

            OnStageStarted?.Invoke();
            NotifyStageChanged();
            StartCoroutine(BootstrapStageDiagnosticCopy());
        }

        private void NotifyStageChanged()
        {
            OnStageChanged?.Invoke(stage);
        }

        private IEnumerator BootstrapStageDiagnosticCopy()
        {
            yield return null;

            if (roleSwapMode == SceneRoleSwapMode.CutSceneRoundTrip &&
                !simultaneousOperators &&
                stage == SceneSessionStage.PlayerBOperator)
            {
                ApplyRoleSwitchDiagnosticBodies();
                yield break;
            }

            ApplyIntroDiagnosticBodies();
        }

        private void ApplyIntroDiagnosticBodies()
        {
            if (useFormattedOperationGuides)
            {
                TrySetInstructionBody(
                    playerADiagnosticDisplay,
                    PanelOperationGuideFormatter.BuildOperatorGuide(AllowedPlayerTag.Player_A),
                    "playerADiagnosticDisplay is not assigned; skipping intro body copy.");
                TrySetInstructionBody(
                    playerBDiagnosticDisplay,
                    PanelOperationGuideFormatter.BuildReaderGuide(AllowedPlayerTag.Player_B),
                    "playerBDiagnosticDisplay is not assigned; skipping intro body copy.");
                return;
            }

            TrySetInstructionBody(playerADiagnosticDisplay, playerAIntroBody, "playerADiagnosticDisplay is not assigned; skipping intro body copy.");
            TrySetInstructionBody(playerBDiagnosticDisplay, playerBIntroBody, "playerBDiagnosticDisplay is not assigned; skipping intro body copy.");
        }

        private IEnumerator ApplyRoleSwitchBodiesAfterDelay()
        {
            yield return null;
            ApplyRoleSwitchDiagnosticBodies();
        }

        private void ApplyRoleSwitchDiagnosticBodies()
        {
            if (useFormattedOperationGuides)
            {
                TrySetInstructionBody(
                    playerADiagnosticDisplay,
                    PanelOperationGuideFormatter.BuildReaderGuide(AllowedPlayerTag.Player_A),
                    "playerADiagnosticDisplay is not assigned; skipping post-solve body copy.");
                TrySetInstructionBody(
                    playerBDiagnosticDisplay,
                    PanelOperationGuideFormatter.BuildOperatorGuide(AllowedPlayerTag.Player_B),
                    "playerBDiagnosticDisplay is not assigned; skipping post-solve body copy.");
                return;
            }

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

            if (simultaneousOperators)
            {
                TryCompleteWhenBothSolved();
                return;
            }

            if (stage != SceneSessionStage.PlayerAOperator)
            {
                return;
            }

            if (roleSwapMode == SceneRoleSwapMode.CutSceneRoundTrip)
            {
                RaisePhaseOneSolvedOnce();
                return;
            }

            stage = SceneSessionStage.PlayerBOperator;
            ApplyStageVisualAndLocks();
            NotifyStageChanged();
            StartCoroutine(ApplyRoleSwitchBodiesAfterDelay());
        }

        private void RaisePhaseOneSolvedOnce()
        {
            if (phaseOneSolvedRaised)
            {
                return;
            }

            phaseOneSolvedRaised = true;
            Debug.Log($"{LogPrefix} Phase 1 solved (cut-scene round-trip mode). Raising OnPhaseOneSolved.", this);
            OnPhaseOneSolved?.Invoke();
        }

        private void HandlePlayerBAttempt(MultiDimensionAttemptResult result)
        {
            if (result == null || !result.IsSolved)
            {
                return;
            }

            if (simultaneousOperators)
            {
                TryCompleteWhenBothSolved();
                return;
            }

            if (stage != SceneSessionStage.PlayerBOperator)
            {
                return;
            }

            stage = SceneSessionStage.Complete;
            ApplyStageVisualAndLocks();
            NotifyStageChanged();
            RaiseCompletionOnce();
        }

        private void TryCompleteWhenBothSolved()
        {
            if (completionRaised)
            {
                return;
            }

            bool aSolved = playerAPuzzleManager != null && playerAPuzzleManager.Solved;
            bool bSolved = playerBPuzzleManager != null && playerBPuzzleManager.Solved;
            if (!aSolved || !bSolved)
            {
                return;
            }

            stage = SceneSessionStage.Complete;
            ApplyStageVisualAndLocks();
            NotifyStageChanged();
            RaiseCompletionOnce();
        }

        private void ApplySimultaneousOperatorLocks()
        {
            if (playerAPanelLock == null || playerBPanelLock == null)
            {
                Debug.LogWarning($"{LogPrefix} Missing playerAPanelLock or playerBPanelLock on '{name}'.", this);
                return;
            }

            playerAPanelLock.ApplyOperatorState();
            playerBPanelLock.ApplyOperatorState();
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
            OnStageCompleted?.Invoke();
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
                case SceneSessionStage.PlayerAOperator:
                    playerAPanelLock.ApplyOperatorState();
                    playerBPanelLock.ApplyWaitingState();
                    break;
                case SceneSessionStage.PlayerBOperator:
                    playerAPanelLock.ApplyWaitingState();
                    playerBPanelLock.ApplyOperatorState();
                    break;
                case SceneSessionStage.Complete:
                    playerAPanelLock.ApplyCompleteLock();
                    playerBPanelLock.ApplyCompleteLock();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
