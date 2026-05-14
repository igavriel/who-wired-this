using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using WhoWiredThis.PanelFocus;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Tutorial
{
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
            "WAITING FOR YOUR TURN\nREAD DIAGNOSTIC\nTALK TO YOUR PARTNER";

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
    /// Staged tutorial flow on top of two MultiDimensionPuzzelManager instances. Does not change puzzle logic.
    /// Runs after InitialPanelFocusBootstrap via DefaultExecutionOrder.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class TutorialStageManager : MonoBehaviour
    {
        private const string LogPrefix = "[TutorialStageManager]";

        private enum TutorialStage
        {
            PlayerAOperator = 0,
            PlayerBOperator = 1,
            Complete = 2
        }

        [Header("Blue / Red = UI labels only — maps to Player A / B")]
        [SerializeField]
        private MultiDimensionPuzzelManager playerAPuzzleManager;

        [SerializeField]
        private MultiDimensionPuzzelManager playerBPuzzleManager;

        [SerializeField]
        private TutorialPanelLockBundle playerAPanelLock;

        [SerializeField]
        private TutorialPanelLockBundle playerBPanelLock;

        [Header("Completion hook (no UI in this task)")]
        [SerializeField]
        private UnityEvent onTutorialCompletedUnity;

        private TutorialStage stage = TutorialStage.PlayerAOperator;
        private bool completionRaised;

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
        }

        private void HandlePlayerAAttempt(MultiDimensionAttemptResult result)
        {
            if (result == null || !result.IsSolved)
            {
                return;
            }

            if (stage != TutorialStage.PlayerAOperator)
            {
                return;
            }

            stage = TutorialStage.PlayerBOperator;
            ApplyStageVisualAndLocks();
        }

        private void HandlePlayerBAttempt(MultiDimensionAttemptResult result)
        {
            if (result == null || !result.IsSolved)
            {
                return;
            }

            if (stage != TutorialStage.PlayerBOperator)
            {
                return;
            }

            stage = TutorialStage.Complete;
            ApplyStageVisualAndLocks();
            RaiseCompletionOnce();
        }

        private void RaiseCompletionOnce()
        {
            if (completionRaised)
            {
                return;
            }

            completionRaised = true;
            OnTutorialCompleted?.Invoke();
            onTutorialCompletedUnity?.Invoke();
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
                case TutorialStage.PlayerAOperator:
                    playerAPanelLock.ApplyOperatorState();
                    playerBPanelLock.ApplyWaitingState();
                    break;
                case TutorialStage.PlayerBOperator:
                    playerAPanelLock.ApplyWaitingState();
                    playerBPanelLock.ApplyOperatorState();
                    break;
                case TutorialStage.Complete:
                    playerAPanelLock.ApplyCompleteLock();
                    playerBPanelLock.ApplyCompleteLock();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
