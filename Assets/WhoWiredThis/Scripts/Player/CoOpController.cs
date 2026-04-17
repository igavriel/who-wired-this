using UnityEngine;

namespace WhoWiredThis.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class CoOpController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private KeyCode moveForward = KeyCode.W;
        [SerializeField] private KeyCode moveBack = KeyCode.S;
        [SerializeField] private KeyCode moveLeft = KeyCode.A;
        [SerializeField] private KeyCode moveRight = KeyCode.D;
        [SerializeField] private KeyCode sprint = KeyCode.LeftShift;
        [SerializeField] private KeyCode interact = KeyCode.LeftControl;

        [Header("Movement")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float sprintSpeed = 6.5f;
        [SerializeField] private float rotationLerpSpeed = 10f;
        [SerializeField] private float gravity = -20f;

        private CharacterController _controller;
        private float _verticalVelocity;

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

            if (Input.GetKey(moveLeft)) horizontal -= 1f;
            if (Input.GetKey(moveRight)) horizontal += 1f;
            if (Input.GetKey(moveBack)) vertical -= 1f;
            if (Input.GetKey(moveForward)) vertical += 1f;

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
            return Input.GetKey(sprint) ? sprintSpeed : moveSpeed;
        }
    }
}
