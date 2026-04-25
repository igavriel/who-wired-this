using UnityEngine;
using UnityEngine.Assertions;
using ThirdPersonMixamo;
using WhoWiredThis.Interfaces;
using WhoWiredThis.Core;
using WhoWiredThis.UI;

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

        [Tooltip("Reference to the PlayerController component for reading player states.")]
        [SerializeField] private PlayerController playerController;

        private IInteractable currentInteractable;

        void Awake()
        {
            Assert.IsNotNull(inputBridge, "PlayerInputBridge is required for PlayerActions");
            Assert.IsNotNull(playerController, "PlayerController is required for PlayerActions");
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
                HUDController.Instance?.SetInteractPrompt(FormatPromptForPlayer(nearest?.GetPromptText()));
            }

            bool activateFromInput = playerController.InteractPressedThisFrame;
            if (activateFromInput && currentInteractable != null)
            {
                currentInteractable.Interact(GetInteractorObject());
                HUDController.Instance?.SetInteractPrompt(FormatPromptForPlayer(currentInteractable.GetPromptText()));
            }
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
            return playerController.InteractKey != KeyCode.None
                ? playerController.InteractKey.ToString()
                : "?";
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
