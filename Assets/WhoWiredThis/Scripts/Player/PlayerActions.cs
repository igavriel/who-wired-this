using UnityEngine;
using UnityEngine.Assertions;
using ThirdPersonMixamo;
using WhoWiredThis.Interfaces;
using WhoWiredThis.Core;
using WhoWiredThis.UI;
using FirstPerson;

namespace WhoWiredThis.Player
{
    public class PlayerActions : MonoBehaviour
    {
        [Header("Detection")]
        [Tooltip("Position used as the center point for interaction checks.")]
        [SerializeField] private Transform detectorOrigin;
        [Tooltip("Maximum distance for detecting interactables.")]
        [SerializeField] private float interactRange = 2.5f;
        [Tooltip("Physics layers included in the nearby-collider scan.")]
        [SerializeField] private LayerMask detectionMask = ~0;

        [Header("References")]
        [Tooltip("Reference to the PlayerInputBridge component for reading input states.")]
        [SerializeField] private PlayerInputBridge inputBridge;

        [Header("Player Controllers - Choose only one")]
        [Tooltip("Reference to the ThirdPersonMixamo.PlayerController component for reading player states.")]
        [SerializeField] private PlayerController playerController = null;
        [Tooltip("Reference to the FirstPerson.FirstPersonController component for reading player states.")]
        [SerializeField] private FirstPersonController firstPersonController = null;

        [Header("HUD (optional)")]
        [Tooltip("Per-player HUD for interact prompts. When unset, uses HUDController.Instance (legacy scenes).")]
        [SerializeField] private PlayerHudView playerHudView;

        private IInteractable currentInteractable;

        void Awake()
        {
            Assert.IsNotNull(inputBridge, "PlayerInputBridge is required for PlayerActions");
        }

        void Start()
        {
            EnsureCursorVisible();
            HUDController.Instance?.SetInteractKeyLabel(GetInteractKeyLabel());
        }

        void Update()
        {
            // Another component may lock cursor after focus changes; keep UI cursor usable.
            EnsureCursorVisible();
            HandleInventoryHotkeys();
            HandleInteraction();
            HandleUIHotkeys();
        }

        void OnDisable()
        {
            ClearInteractPrompt();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                EnsureCursorVisible();
            }
        }

        private void HandleInventoryHotkeys()
        {
            bool slot1Pressed = inputBridge.Slot1PressedThisFrame;
            bool slot2Pressed = inputBridge.Slot2PressedThisFrame;
            bool slot3Pressed = inputBridge.Slot3PressedThisFrame;

            if (slot1Pressed)
            {
                InventoryManager.Instance?.SelectIndex(0);
            }

            if (slot2Pressed)
            {
                InventoryManager.Instance?.SelectIndex(1);
            }

            if (slot3Pressed)
            {
                InventoryManager.Instance?.SelectIndex(2);
            }
        }

        private void HandleInteraction()
        {
            bool activateFromInput =
                playerController != null ? playerController.InteractPressedThisFrame
                : firstPersonController != null ? firstPersonController.InteractPressedThisFrame
                : inputBridge.InteractPressedThisFrame;

            if (activateFromInput && playerHudView != null && playerHudView.IsPopupOpen)
            {
                playerHudView.HidePopup();
                return;
            }

            Vector3 origin = GetOriginPosition();
            Collider[] nearbyColliders = Physics.OverlapSphere(origin, interactRange, detectionMask);
            IInteractable nearest = null;
            float nearestSqrDistance = float.MaxValue;

            for (int i = 0; i < nearbyColliders.Length; i++)
            {
                Collider collider = nearbyColliders[i];
                if (collider == null)
                {
                    continue;
                }

                IInteractable interactable = collider.GetComponent<IInteractable>()
                    ?? collider.GetComponentInParent<IInteractable>();

                if (!(interactable is Component interactableComponent))
                {
                    continue;
                }

                float sqrDistance = (interactableComponent.transform.position - origin).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearest = interactable;
                }
            }

            if (nearest != currentInteractable)
            {
                currentInteractable = nearest;
                SetInteractPromptForHud(FormatPromptForPlayer(nearest?.GetPromptText()));
            }

            if (activateFromInput && currentInteractable != null)
            {
                currentInteractable.Interact(GetInteractorObject());
                SetInteractPromptForHud(FormatPromptForPlayer(currentInteractable.GetPromptText()));
            }
        }

        private void SetInteractPromptForHud(string text)
        {
            if (playerHudView != null)
            {
                playerHudView.SetInteractPrompt(text);
                return;
            }

            HUDController.Instance?.SetInteractPrompt(text);
        }

        private void ClearInteractPrompt()
        {
            if (playerHudView != null)
            {
                playerHudView.ClearInteractPrompt();
                return;
            }

            HUDController.Instance?.SetInteractPrompt(null);
        }

        private string FormatPromptForPlayer(string prompt)
        {
            if (string.IsNullOrEmpty(prompt))
            {
                return prompt;
            }

            string interactKeyLabel = GetInteractKeyLabel();
            string interactToken = $"[{interactKeyLabel}]";

            return prompt.Replace("$INTERACT$", interactToken);
        }

        private string GetInteractKeyLabel()
        {
            if (playerController != null)
            {
                return playerController.InteractKey != KeyCode.None
                    ? playerController.InteractKey.ToString()
                    : "?";
            }
            if (firstPersonController != null)
            {
                return firstPersonController.InteractKey != KeyCode.None
                    ? firstPersonController.InteractKey.ToString()
                    : "?";
            }
            return "?";
        }

        private void HandleUIHotkeys()
        {
            bool inventoryPressed = inputBridge.InventoryPressedThisFrame;
            bool helpPressed = inputBridge.HelpPressedThisFrame;
            bool menuPressed = inputBridge.MenuPressedThisFrame;

            if (inventoryPressed)
            {
                HUDController.Instance?.ToggleInventory();
            }

            if (helpPressed)
            {
                HUDController.Instance?.ToggleHelp();
            }

            if (menuPressed)
            {
                HUDController.Instance?.ToggleMenuPanel();
            }
        }

        private GameObject GetInteractorObject()
        {
            return detectorOrigin != null ? detectorOrigin.gameObject : gameObject;
        }

        private Vector3 GetOriginPosition()
        {
            return detectorOrigin != null ? detectorOrigin.position : transform.position;
        }

        private static void EnsureCursorVisible()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(GetOriginPosition(), interactRange);
        }
    }
}
