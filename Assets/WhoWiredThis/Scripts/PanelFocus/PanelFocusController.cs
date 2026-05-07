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

        [Header("Camera Framing")]
        [Tooltip("Percent of the screen height/width occupied by the full board frame while focused.")]
        [Range(10f, 100f)]
        [SerializeField] private float frameFillPercent = 95f;
        [Tooltip("Optional board renderer used for framing. If empty, uses Renderer on this GameObject.")]
        [SerializeField] private Renderer boardRenderer;
        [Tooltip("Small safety offset added to the computed camera distance.")]
        [SerializeField] private float extraDistance = 0.02f;

        [Header("Buttons")]
        [Tooltip("All non-exit buttons in visual left-to-right order.")]
        [SerializeField] private PanelFocusButton[] interactableButtons;
        [Tooltip("Always-present Solve button.")]
        [SerializeField] private PanelFocusButton solveButton;
        [Tooltip("Always-present Exit button.")]
        [SerializeField] private PanelFocusButton exitButton;

        [Header("Selection Frame")]
        [Tooltip("Border-image object re-parented under selected HighlightAnchor.")]
        [SerializeField] private GameObject selectionFrame;

        [Header("Prompt")]
        [SerializeField] private string promptText = "$INTERACT$ Open Panel";

        private int selectedIndex;
        private PlayerPanelFocusController activeController;

        public AllowedPlayerTag AllowedPlayerId => allowedPlayerId;
        private int ButtonCount => interactableButtons != null ? interactableButtons.Length : 0;
        private int SolveIndex => ButtonCount;
        private int ExitIndex => ButtonCount + 1;
        private int TotalCount => ButtonCount + 2; // + dedicated solve and exit buttons
        private bool IsSolveSelected => selectedIndex == SolveIndex;
        private bool IsExitSelected => selectedIndex == ExitIndex;

        public string GetPromptText() => promptText;

        private void Awake()
        {
            if (solveButton == null || solveButton.HighlightAnchor == null)
            {
                Debug.LogWarning($"[PanelFocusController] Solve button / HighlightAnchor is missing on {name}.", this);
            }

            if (exitButton == null || exitButton.HighlightAnchor == null)
            {
                Debug.LogWarning($"[PanelFocusController] Exit button / HighlightAnchor is missing on {name}.", this);
            }

            ValidateButtonWiring();
        }

        public void GetCameraSnapPose(Camera playerCamera, out Vector3 worldPos, out Quaternion worldRot)
        {
            Renderer targetRenderer = boardRenderer != null ? boardRenderer : GetComponent<Renderer>();
            float fill = Mathf.Clamp(frameFillPercent / 100f, 0.1f, 1f);
            Quaternion boardRotation = transform.rotation; // no tilt relative to board
            Vector3 boardCenter = targetRenderer != null ? targetRenderer.bounds.center : transform.position;

            if (playerCamera == null || targetRenderer == null)
            {
                worldRot = boardRotation;
                worldPos = boardCenter - (worldRot * Vector3.forward) * (1f + Mathf.Max(0f, extraDistance));
                return;
            }

            Vector3 localExtents = transform.InverseTransformVector(targetRenderer.bounds.extents);
            float halfWidth = Mathf.Abs(localExtents.x);
            float halfHeight = Mathf.Abs(localExtents.y);

            float verticalHalfFovRad = 0.5f * playerCamera.fieldOfView * Mathf.Deg2Rad;
            float horizontalHalfFovRad = Mathf.Atan(Mathf.Tan(verticalHalfFovRad) * playerCamera.aspect);

            float distanceForHeight = halfHeight / (Mathf.Tan(verticalHalfFovRad) * fill);
            float distanceForWidth = halfWidth / (Mathf.Tan(horizontalHalfFovRad) * fill);
            float distance = Mathf.Max(distanceForHeight, distanceForWidth) + Mathf.Max(0f, extraDistance);

            worldRot = boardRotation;
            worldPos = boardCenter - (worldRot * Vector3.forward) * distance;
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
            if (selectionFrame != null)
            {
                selectionFrame.SetActive(true);
            }
            RefreshSelectionFrame();
        }

        public void OnFocusExited()
        {
            activeController = null;
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
                Debug.Log($"Player {activeController.PlayerId} pressed ExitButton.");
                activeController.ExitFocus();
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
