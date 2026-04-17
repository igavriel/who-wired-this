using System;
using UnityEngine;

namespace ThirdPersonMixamo
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private PlayerControlBindings inputBindings;

        [Header("Movement")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float sprintSpeed = 6.5f;
        [SerializeField] private float rotationLerpSpeed = 10f;
        [SerializeField] private float gravity = -20f;

        [Header("Jump")]
        [SerializeField] private float jumpHeight = 1.2f;

        private CharacterController _controller;
        private float _verticalVelocity;
        private bool _wasGrounded = true;

        public event Action JumpStarted;
        public event Action Landed;

        private KeyCode MoveForwardKey => inputBindings != null ? inputBindings.MoveForward : KeyCode.W;
        private KeyCode MoveBackKey => inputBindings != null ? inputBindings.MoveBack : KeyCode.S;
        private KeyCode MoveLeftKey => inputBindings != null ? inputBindings.MoveLeft : KeyCode.A;
        private KeyCode MoveRightKey => inputBindings != null ? inputBindings.MoveRight : KeyCode.D;
        private KeyCode SprintKey => inputBindings != null ? inputBindings.Sprint : KeyCode.LeftShift;
        private KeyCode JumpKey => inputBindings != null ? inputBindings.Jump : KeyCode.Space;

        public CharacterController CharacterController => _controller;
        public bool IsGrounded => _controller != null && _controller.isGrounded;
        public Vector3 Velocity => _controller != null ? _controller.velocity : Vector3.zero;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            bool groundedBeforeMove = _controller.isGrounded;

            Vector3 moveDirection = GetMoveDirectionFromInput();
            float targetSpeed = GetTargetSpeed();
            Vector3 velocity = moveDirection * targetSpeed;

            if (groundedBeforeMove && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            if (groundedBeforeMove && Input.GetKeyDown(JumpKey))
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                JumpStarted?.Invoke();
            }

            _verticalVelocity += gravity * Time.deltaTime;
            velocity.y = _verticalVelocity;

            _controller.Move(velocity * Time.deltaTime);

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerpSpeed * Time.deltaTime);
            }

            bool groundedAfterMove = _controller.isGrounded;
            if (groundedAfterMove && !_wasGrounded)
            {
                Landed?.Invoke();
            }

            _wasGrounded = groundedAfterMove;
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
