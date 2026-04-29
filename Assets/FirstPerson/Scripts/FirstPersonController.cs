using UnityEngine;
using UnityEngine.Assertions;
using WhoWiredThis.Interfaces;

namespace FirstPerson
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private PlayerControlBindings inputBindings;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float turnSpeed = 180f;
        [SerializeField] private float gravity = 20f;

        [Header("Camera")]
        [SerializeField] private Camera playerCamera;

        [Header("Interact")]
        [SerializeField] private float interactDistance = 3f;
        [SerializeField] private LayerMask interactMask = ~0;

        private CharacterController _characterController;
        private float _verticalVelocity;

        private KeyCode MoveForwardKey => inputBindings.MoveForward;
        private KeyCode MoveBackKey => inputBindings.MoveBack;
        private KeyCode MoveLeftKey => inputBindings.MoveLeft;
        private KeyCode MoveRightKey => inputBindings.MoveRight;
        private KeyCode InteractKey => inputBindings.Interact;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            Assert.IsNotNull(_characterController, "[FirstPerson] CharacterController is required.");
            Assert.IsNotNull(inputBindings, "[FirstPerson] Input bindings are required.");
            Assert.IsNotNull(playerCamera, "[FirstPerson] Player camera reference is required.");
        }

        private void Update()
        {
            float turnInput = 0f;
            float moveInput = 0f;
            if (Input.GetKey(MoveLeftKey)) turnInput -= 1f;
            if (Input.GetKey(MoveRightKey)) turnInput += 1f;
            if (Input.GetKey(MoveBackKey)) moveInput -= 1f;
            if (Input.GetKey(MoveForwardKey)) moveInput += 1f;

            if (Mathf.Abs(turnInput) > 0.01f)
            {
                float yawDelta = turnInput * turnSpeed * Time.deltaTime;
                transform.Rotate(0f, yawDelta, 0f, Space.Self);
            }

            Vector3 forward = playerCamera.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 worldMove = forward * (moveInput * moveSpeed);

            if (_characterController.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -1f;
            }

            _verticalVelocity -= gravity * Time.deltaTime;
            worldMove.y = _verticalVelocity;
            _characterController.Move(worldMove * Time.deltaTime);

            if (!Input.GetKeyDown(InteractKey))
            {
                return;
            }

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Collide))
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                interactable?.Interact(gameObject);
            }
        }
    }
}
