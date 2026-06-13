using UnityEngine;
using FirstPerson;
using WhoWiredThis.Enums;
using WhoWiredThis.Player;

namespace WhoWiredThis.PanelFocus
{
    /// <summary>
    /// Per-player focus mode driver. Reuses the player's existing PlayerControlBindings SO
    /// for input and disables the FirstPersonController during focus to suppress movement,
    /// turning, and the controller's own forward-raycast Interact.
    /// Camera is snapped to the panel's FocusCameraAnchor and restored on exit.
    /// </summary>
    public class PlayerPanelFocusController : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private AllowedPlayerTag playerId = AllowedPlayerTag.Player_A;

        [Header("References")]
        [Tooltip("Camera child of this player; transform is snapped to a panel anchor while focused.")]
        [SerializeField] private Camera playerCamera;

        [Tooltip("This player's FirstPersonController; disabled while focused.")]
        [SerializeField] private FirstPersonController firstPersonController;

        [Tooltip("Per-player input keybindings ScriptableObject (PlayerControlBindings_PlayerA / _PlayerB).")]
        [SerializeField] private PlayerControlBindings inputBindings;

        [Tooltip("PlayerActions is also disabled during focus to avoid stale Interact re-triggers.")]
        [SerializeField] private PlayerActions playerActions;

        private bool isFocused;
        private PanelFocusController currentPanel;
        private Vector3 cachedCameraLocalPosition;
        private Quaternion cachedCameraLocalRotation;
        private int lastStateChangeFrame = int.MinValue;

        public AllowedPlayerTag PlayerId => playerId;
        public bool IsFocused => isFocused;

        // Prevent same-frame double-triggers: the Interact press that enters focus
        // must not also activate a target this frame, and the press that exits focus
        // must not be re-consumed by FirstPersonController to re-enter immediately.
        private bool IsInputAllowedThisFrame => Time.frameCount > lastStateChangeFrame;

        public bool TryEnterFocus(PanelFocusController panel)
        {
            if (isFocused || panel == null || !IsInputAllowedThisFrame)
            {
                return false;
            }

            if (panel.AllowedPlayerId != playerId)
            {
                return false;
            }

            if (playerCamera == null)
            {
                Debug.LogWarning($"[PlayerPanelFocusController] Missing player camera reference on {name}.", this);
                return false;
            }

            cachedCameraLocalPosition = playerCamera.transform.localPosition;
            cachedCameraLocalRotation = playerCamera.transform.localRotation;

            panel.GetCameraSnapPose(playerCamera, out Vector3 snapPosition, out Quaternion snapRotation);
            playerCamera.transform.SetPositionAndRotation(snapPosition, snapRotation);

            if (firstPersonController != null)
            {
                firstPersonController.enabled = false;
            }

            if (playerActions != null)
            {
                playerActions.ClearInteractPrompt();
                playerActions.enabled = false;
            }

            currentPanel = panel;
            isFocused = true;
            lastStateChangeFrame = Time.frameCount;
            panel.OnFocusEntered(this);
            return true;
        }

        public void ExitFocus()
        {
            if (!isFocused)
            {
                return;
            }

            lastStateChangeFrame = Time.frameCount;

            if (playerCamera != null)
            {
                playerCamera.transform.localPosition = cachedCameraLocalPosition;
                playerCamera.transform.localRotation = cachedCameraLocalRotation;
            }

            if (firstPersonController != null)
            {
                firstPersonController.enabled = true;
            }
            if (playerActions != null)
            {
                playerActions.enabled = true;
            }

            PanelFocusController exitingPanel = currentPanel;
            currentPanel = null;
            isFocused = false;
            exitingPanel?.OnFocusExited();
        }

        private void Update()
        {
            if (!isFocused || currentPanel == null || inputBindings == null || !IsInputAllowedThisFrame)
            {
                return;
            }

            if (Input.GetKeyDown(inputBindings.MoveLeft))
            {
                currentPanel.MoveSelection(-1);
            }
            else if (Input.GetKeyDown(inputBindings.MoveRight))
            {
                currentPanel.MoveSelection(+1);
            }

            if (IsActionPressedThisFrame())
            {
                if (TryDismissPopupIfOpen())
                {
                    return;
                }

                currentPanel.ActivateSelected(gameObject);
            }
        }

        private bool IsActionPressedThisFrame()
        {
            return Input.GetKeyDown(inputBindings.Interact)
                   || Input.GetKeyDown(inputBindings.MoveForward)
                   || Input.GetKeyDown(inputBindings.MoveBack);
        }

        private bool TryDismissPopupIfOpen()
        {
            if (playerActions == null || playerActions.PlayerHud == null || !playerActions.PlayerHud.IsPopupOpen)
            {
                return false;
            }

            playerActions.PlayerHud.HidePopup();
            return true;
        }
    }
}
