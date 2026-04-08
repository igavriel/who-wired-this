using UnityEngine;
using UnityEngine.EventSystems;
using WhoWiredThis.Interactables;
using WhoWiredThis.Inventory;
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
        [SerializeField] private PlayerInputBridge inputBridge;

        private IInteractable currentInteractable;

        void Awake()
        {
            if (inputBridge == null)
            {
                inputBridge = GetComponent<PlayerInputBridge>();
            }
        }

        void Start()
        {
            EnsureCursorVisible();
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
            bool slot1Pressed = inputBridge != null
                ? inputBridge.Slot1PressedThisFrame
                : Input.GetKeyDown(KeyCode.Alpha1);
            bool slot2Pressed = inputBridge != null
                ? inputBridge.Slot2PressedThisFrame
                : Input.GetKeyDown(KeyCode.Alpha2);
            bool slot3Pressed = inputBridge != null
                ? inputBridge.Slot3PressedThisFrame
                : Input.GetKeyDown(KeyCode.Alpha3);

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
                HUDController.Instance?.SetInteractPrompt(nearest?.GetPromptText());
            }

            bool activateFromInput = inputBridge != null
                ? inputBridge.InteractPressedThisFrame
                : Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0);
            bool activateFromMouse = inputBridge != null
                ? inputBridge.InteractPressedFromPointerThisFrame
                : Input.GetMouseButtonDown(0);
            bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            if (activateFromMouse && pointerOverUi)
            {
                return;
            }

            if (activateFromInput && currentInteractable != null)
            {
                currentInteractable.Interact(GetInteractorObject());
            }
        }

        private void HandleUIHotkeys()
        {
            bool inventoryPressed = inputBridge != null
                ? inputBridge.InventoryPressedThisFrame
                : Input.GetKeyDown(KeyCode.I);
            bool helpPressed = inputBridge != null
                ? inputBridge.HelpPressedThisFrame
                : Input.GetKeyDown(KeyCode.H);
            bool menuPressed = inputBridge != null && inputBridge.MenuPressedThisFrame;

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
