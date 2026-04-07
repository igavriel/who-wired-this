using UnityEngine;
using WhoWiredThis.Interactables;
using WhoWiredThis.Inventory;
using WhoWiredThis.UI;

namespace WhoWiredThis.Player
{
    // Requires the Active Input Handling in Project Settings > Player to be
    // "Input Manager (Old)" or "Both" since this uses UnityEngine.Input.
    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 5f;
        public float gravity = -18f;
        public float rotationSpeed = 12f;

        [Header("Interaction")]
        public float interactRange = 2.5f;
        // Set in Inspector to the "Interactable" layer (or leave as Default / Everything for POC)
        public LayerMask interactableLayer = ~0;

        private CharacterController cc;
        private Vector3 verticalVelocity;
        private IInteractable currentInteractable;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
        }

        void Update()
        {
            HandleMovement();
            HandleInventoryHotkeys();
            HandleInteraction();

            if (Input.GetKeyDown(KeyCode.I))
            {
                HUDController.Instance?.ToggleInventory();
            }
        }

        void HandleMovement()
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            Camera cam = Camera.main;
            Vector3 forward = cam != null
                ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized
                : Vector3.forward;
            Vector3 right = cam != null
                ? Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized
                : Vector3.right;

            Vector3 move = forward * v + right * h;

            if (move.sqrMagnitude > 0.01f)
            {
                Quaternion target = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
                cc.Move(move.normalized * moveSpeed * Time.deltaTime);
            }

            if (cc.isGrounded && verticalVelocity.y < 0f)
            {
                verticalVelocity.y = -2f;
            }

            verticalVelocity.y += gravity * Time.deltaTime;
            cc.Move(verticalVelocity * Time.deltaTime);
        }

        void HandleInventoryHotkeys()
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

        void HandleInteraction()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactableLayer);
            IInteractable nearest = null;
            float nearestDist = float.MaxValue;

            foreach (Collider col in hits)
            {
                IInteractable interactable = col.GetComponent<IInteractable>()
                    ?? col.GetComponentInParent<IInteractable>();

                if (interactable == null)
                {
                    continue;
                }

                float dist = Vector3.Distance(transform.position, col.transform.position);

                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = interactable;
                }
            }

            if (nearest != currentInteractable)
            {
                currentInteractable = nearest;
                HUDController.Instance?.SetInteractPrompt(nearest?.GetPromptText());
            }

            if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null)
            {
                currentInteractable.Interact(gameObject);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
