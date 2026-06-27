using System;
using UnityEngine;
using WhoWiredThis.Enums;
using WhoWiredThis.Interfaces;
using WhoWiredThis.Player;
using WhoWiredThis.Util;

namespace WhoWiredThis.PanelFocus
{
    [Serializable]
    public class PanelFocusButton
    {
        [SerializeField] private string label = "Button";
        [SerializeField] private Transform highlightAnchor;
        [Tooltip("Reference must implement IInteractable.")]
        [RequireInterface(typeof(IInteractable))]
        [SerializeField] private MonoBehaviour interactableReference;

        public string Label => label;
        public Transform HighlightAnchor => highlightAnchor;
        public IInteractable Interactable => interactableReference as IInteractable;
    }

    /// <summary>
    /// Per-panel controller that participates in the existing IInteractable flow and
    /// drives selection among variable buttons plus a dedicated Exit slot.
    /// </summary>
    public class PanelFocusController : MonoBehaviour, IInteractable
    {
        [Header("Ownership")]
        [SerializeField] private AllowedPlayerTag allowedPlayerId = AllowedPlayerTag.Player_A;

        [Header("Buttons")]
        [Tooltip("All non-exit buttons in visual left-to-right order.")]
        [SerializeField]
        private PanelFocusButton[] interactableButtons;

        [Tooltip("Always-present Solve button.")]
        [SerializeField]
        private PanelFocusButton solveButton;

        [Tooltip("Always-present Exit button.")]
        [SerializeField]
        private PanelFocusButton exitButton;

        [Header("Exit")]
        [Tooltip("When off, Exit is omitted from the focus selection cycle (Solve remains).")]
        [SerializeField]
        private bool includeExitInFocusCycle = true;

        [Tooltip("Second Activate on Exit within this interval (seconds) exits panel focus.")]
        [SerializeField]
        [Min(0.05f)]
        private float exitDoubleClickMaxIntervalSeconds = 0.5f;

        [Header("Optional action gate")]
        [Tooltip("When locked, keyboard Activate ignores every slot except Exit. Leave empty to resolve a PanelActionLock on a parent (e.g. panel root).")]
        [SerializeField]
        private PanelActionLock panelActionLock;

        [Header("Selection Frame")]
        [Tooltip("Border-image object re-parented under selected HighlightAnchor.")]
        [SerializeField]
        private GameObject selectionFrame;

        [Header("Prompt")]
        [SerializeField]
        private string promptText = "$INTERACT$ Open Panel";

        [Header("Focus Visibility")]
        [Tooltip("Objects hidden while this panel is in focus, restored on exit.")]
        [SerializeField]
        private GameObject[] hideOnFocusObjects;

        private const float NoPendingExitClick = -1f;

        private int selectedIndex;
        private PlayerPanelFocusController activeController;
        private float lastExitActivateUnscaledTime = NoPendingExitClick;
        private bool[] hideOnFocusPreviousStates;

        public AllowedPlayerTag AllowedPlayerId => allowedPlayerId;
        private int ButtonCount => interactableButtons != null ? interactableButtons.Length : 0;
        private int SolveIndex => ButtonCount;
        private int ExitIndex => ButtonCount + 1;
        private int TotalCount => ButtonCount + (includeExitInFocusCycle ? 2 : 1);
        private bool IsSolveSelected => selectedIndex == SolveIndex;
        private bool IsExitSelected => includeExitInFocusCycle && selectedIndex == ExitIndex;

        public string GetPromptText() => promptText;

        private void Awake()
        {
            if (solveButton == null || solveButton.HighlightAnchor == null)
            {
                Debug.LogWarning($"[PanelFocusController] Solve button / HighlightAnchor is missing on {name}.", this);
            }

            if (includeExitInFocusCycle && (exitButton == null || exitButton.HighlightAnchor == null))
            {
                Debug.LogWarning($"[PanelFocusController] Exit button / HighlightAnchor is missing on {name}.", this);
            }

            ValidateButtonWiring();
        }

        public void Interact(GameObject interactor)
        {
            if (interactor == null || activeController != null)
            {
                return;
            }

            if (!PlayerInteractorResolver.TryResolve(interactor.transform, out AllowedPlayerTag playerTag))
            {
                return;
            }

            if (playerTag != allowedPlayerId)
            {
                return;
            }

            PlayerPanelFocusController focus = interactor.GetComponentInParent<PlayerPanelFocusController>();
            if (focus == null)
            {
                return;
            }

            focus.TryEnterFocus(this);
        }

        public void OnFocusEntered(PlayerPanelFocusController focus)
        {
            activeController = focus;
            selectedIndex = 0;
            ApplyHideOnFocusState(true);
            if (selectionFrame != null)
            {
                selectionFrame.SetActive(true);
            }

            RefreshSelectionFrame();
        }

        public void OnFocusExited()
        {
            activeController = null;
            lastExitActivateUnscaledTime = NoPendingExitClick;
            ApplyHideOnFocusState(false);
            if (selectionFrame != null)
            {
                selectionFrame.SetActive(false);
            }
        }

        public void MoveSelection(int delta)
        {
            if (TotalCount <= 1)
            {
                return;
            }

            selectedIndex += delta;
            if (selectedIndex < 0)
            {
                selectedIndex = TotalCount - 1;
            }
            else if (selectedIndex >= TotalCount)
            {
                selectedIndex = 0;
            }

            RefreshSelectionFrame();
        }

        public void ActivateSelected(GameObject interactor)
        {
            if (activeController == null)
            {
                Debug.LogWarning($"[PanelFocusController] ActivateSelected ignored on '{name}' because no active controller is set.", this);
                return;
            }

            Debug.Log(
                $"[PanelFocusController] ActivateSelected on '{name}' by {activeController.PlayerId}. " +
                $"selectedIndex={selectedIndex}, buttonCount={ButtonCount}, isSolve={IsSolveSelected}, isExit={IsExitSelected}.",
                this);

            if (IsExitSelected)
            {
                if (TryConfirmExitDoubleClick())
                {
                    activeController.ExitFocus();
                }

                return;
            }

            lastExitActivateUnscaledTime = NoPendingExitClick;

            PanelActionLock actionLock = PanelActionLock.Resolve(this, panelActionLock);
            if (actionLock != null && actionLock.IsLocked)
            {
                Debug.LogWarning(
                    $"[PanelFocusController] Action blocked on '{name}' for {activeController.PlayerId} — panel is locked " +
                    $"(waiting for your turn, or puzzle complete). Selection keys still move focus; Ctrl and vertical action keys cannot cycle inputs or Send.",
                    this);
                return;
            }

            if (IsSolveSelected)
            {
                if (solveButton == null)
                {
                    Debug.LogWarning($"[PanelFocusController] Solve selection on '{name}' but solveButton is null.", this);
                    return;
                }

                Debug.Log($"Player {activeController.PlayerId} activated '{solveButton.Label}'.");
                IInteractable solveInteractable = solveButton.Interactable;
                if (solveInteractable == null)
                {
                    Debug.LogWarning($"[PanelFocusController] Solve button on '{name}' has no IInteractable assigned.", this);
                    return;
                }

                Debug.Log(
                    $"[PanelFocusController] Forwarding solve interact from '{name}' to '{(solveInteractable as Component)?.name ?? solveInteractable.GetType().Name}' " +
                    $"({solveInteractable.GetType().Name}).",
                    this);
                solveInteractable.Interact(interactor);
                return;
            }

            if (interactableButtons == null || selectedIndex < 0 || selectedIndex >= interactableButtons.Length)
            {
                return;
            }

            PanelFocusButton entry = interactableButtons[selectedIndex];
            if (entry == null)
            {
                return;
            }

            Debug.Log($"Player {activeController.PlayerId} activated '{entry.Label}'.");
            IInteractable interactable = entry.Interactable;
            if (interactable == null)
            {
                Debug.LogWarning($"[PanelFocusController] Button '{entry.Label}' on '{name}' has no IInteractable assigned.", this);
                return;
            }

            interactable.Interact(interactor);
        }

        private bool TryConfirmExitDoubleClick()
        {
            float now = Time.unscaledTime;
            if (lastExitActivateUnscaledTime >= 0f &&
                now - lastExitActivateUnscaledTime <= exitDoubleClickMaxIntervalSeconds)
            {
                lastExitActivateUnscaledTime = NoPendingExitClick;
                Debug.Log($"Player {activeController.PlayerId} confirmed Exit (double-click).", this);
                return true;
            }

            lastExitActivateUnscaledTime = now;
            Debug.Log(
                $"Player {activeController.PlayerId} pressed ExitButton — press again within {exitDoubleClickMaxIntervalSeconds:F2}s to quit.",
                this);
            return false;
        }

        private void ApplyHideOnFocusState(bool enteringFocus)
        {
            if (hideOnFocusObjects == null || hideOnFocusObjects.Length == 0)
            {
                return;
            }

            if (enteringFocus)
            {
                hideOnFocusPreviousStates = new bool[hideOnFocusObjects.Length];
                for (int i = 0; i < hideOnFocusObjects.Length; i++)
                {
                    GameObject target = hideOnFocusObjects[i];
                    if (target == null)
                    {
                        continue;
                    }

                    hideOnFocusPreviousStates[i] = target.activeSelf;
                    target.SetActive(false);
                }

                return;
            }

            for (int i = 0; i < hideOnFocusObjects.Length; i++)
            {
                GameObject target = hideOnFocusObjects[i];
                if (target == null)
                {
                    continue;
                }

                bool wasActive = hideOnFocusPreviousStates != null && i < hideOnFocusPreviousStates.Length && hideOnFocusPreviousStates[i];
                target.SetActive(wasActive);
            }

            hideOnFocusPreviousStates = null;
        }

        private void ValidateButtonWiring()
        {
            if (interactableButtons == null)
            {
                return;
            }

            for (int i = 0; i < interactableButtons.Length; i++)
            {
                PanelFocusButton entry = interactableButtons[i];
                if (entry == null)
                {
                    Debug.LogWarning($"[PanelFocusController] interactableButtons[{i}] is null on '{name}'.", this);
                    continue;
                }

                if (entry.HighlightAnchor == null)
                {
                    Debug.LogWarning($"[PanelFocusController] Button '{entry.Label}' is missing HighlightAnchor on '{name}'.", this);
                }

                if (entry.Interactable == null)
                {
                    Debug.LogWarning($"[PanelFocusController] Button '{entry.Label}' is missing IInteractable on '{name}'.", this);
                }
            }
        }

        private void RefreshSelectionFrame()
        {
            if (selectionFrame == null)
            {
                return;
            }

            PanelFocusButton current = IsExitSelected
                ? exitButton
                : IsSolveSelected
                    ? solveButton
                    : (interactableButtons != null && selectedIndex >= 0 && selectedIndex < interactableButtons.Length
                        ? interactableButtons[selectedIndex]
                        : null);
            Transform anchor = current?.HighlightAnchor;
            if (anchor == null)
            {
                return;
            }

            Transform frameTransform = selectionFrame.transform;
            frameTransform.SetParent(anchor, false);
            frameTransform.localPosition = Vector3.zero;
            frameTransform.localRotation = Quaternion.identity;
            frameTransform.localScale = Vector3.one;
        }
    }
}
