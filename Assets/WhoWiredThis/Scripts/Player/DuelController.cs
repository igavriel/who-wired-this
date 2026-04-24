using UnityEngine;
using UnityEngine.Events;

namespace WhoWiredThis.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class DuelController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private DuelControlBindings inputBindings;

        [Header("Movement")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float sprintSpeed = 6.5f;
        [SerializeField] private float rotationLerpSpeed = 10f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float jumpHeight = 1.2f;

        [Header("Actions")]
        [SerializeField] private UnityEvent onInteract;

        private CharacterController _controller;
        private float _verticalVelocity;
        private bool _interactPressedThisFrame;

        private KeyCode MoveForwardKey => inputBindings != null ? inputBindings.MoveForward : KeyCode.W;
        private KeyCode MoveBackKey => inputBindings != null ? inputBindings.MoveBack : KeyCode.S;
        private KeyCode MoveLeftKey => inputBindings != null ? inputBindings.MoveLeft : KeyCode.A;
        private KeyCode MoveRightKey => inputBindings != null ? inputBindings.MoveRight : KeyCode.D;
        private KeyCode SprintKey => inputBindings != null ? inputBindings.Sprint : KeyCode.LeftShift;
        private KeyCode JumpKey => inputBindings != null ? inputBindings.Jump : KeyCode.Space;
        private KeyCode InteractKey => inputBindings != null ? inputBindings.Interact : KeyCode.LeftControl;
        public bool InteractPressedThisFrame => _interactPressedThisFrame;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            Vector3 moveDirection = GetMoveDirectionFromInput();
            float targetSpeed = GetTargetSpeed();
            Vector3 velocity = moveDirection * targetSpeed;

            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            if (_controller.isGrounded && Input.GetKeyDown(JumpKey))
            {
                // Same jump math used by other character controllers in project.
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            _interactPressedThisFrame = Input.GetKeyDown(InteractKey);
            if (_interactPressedThisFrame)
            {
                onInteract?.Invoke();
            }

            _verticalVelocity += gravity * Time.deltaTime;
            velocity.y = _verticalVelocity;

            _controller.Move(velocity * Time.deltaTime);

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerpSpeed * Time.deltaTime);
            }
        }

        private Vector3 GetMoveDirectionFromInput()
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (Input.GetKey(MoveLeftKey)) horizontal -= 1f;
            if (Input.GetKey(MoveRightKey)) horizontal += 1f;
            if (Input.GetKey(MoveBackKey)) vertical -= 1f;
            if (Input.GetKey(MoveForwardKey)) vertical += 1f;

            Vector3 inputDirection = new Vector3(horizontal, 0f, vertical);
            if (inputDirection.sqrMagnitude < 0.001f)
            {
                return Vector3.zero;
            }

            inputDirection.Normalize();

            if (cameraTransform == null)
            {
                return inputDirection;
            }

            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            return (forward * inputDirection.z + right * inputDirection.x).normalized;
        }

        private float GetTargetSpeed()
        {
            return Input.GetKey(SprintKey) ? sprintSpeed : moveSpeed;
        }
    }
}
