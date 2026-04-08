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

        private IInteractable currentInteractable;

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
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                InventoryManager.Instance?.SelectIndex(0);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                InventoryManager.Instance?.SelectIndex(1);
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
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

            bool activateFromKeyboard = Input.GetKeyDown(KeyCode.E);
            bool activateFromMouse = Input.GetMouseButtonDown(0);
            bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            if (activateFromMouse && pointerOverUi)
            {
                return;
            }

            if ((activateFromKeyboard || activateFromMouse) && currentInteractable != null)
            {
                currentInteractable.Interact(GetInteractorObject());
            }
        }

        private void HandleUIHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                HUDController.Instance?.ToggleInventory();
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                HUDController.Instance?.ToggleHelp();
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
