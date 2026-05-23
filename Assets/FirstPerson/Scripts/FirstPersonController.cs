using UnityEngine;
using UnityEngine.Assertions;
using WhoWiredThis.Interfaces;
using WhoWiredThis.Player;
using System;

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
        private PlayerActions _playerActions;
        private float _verticalVelocity;

        private bool _inputInteract;

        public bool InteractPressedThisFrame => _inputInteract;

        public KeyCode MoveForwardKey => inputBindings.MoveForward;
        public KeyCode MoveBackKey => inputBindings.MoveBack;
        public KeyCode MoveLeftKey => inputBindings.MoveLeft;
        public KeyCode MoveRightKey => inputBindings.MoveRight;
        public KeyCode InteractKey => inputBindings.Interact;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _playerActions = GetComponent<PlayerActions>();
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

            _inputInteract = Input.GetKeyDown(InteractKey);

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

            // PlayerActions on the same object already performs overlap-based Interact on this key.
            if (_playerActions != null)
            {
                return;
            }

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance, interactMask, QueryTriggerInteraction.Collide);
            if (hits.Length == 0)
            {
                return;
            }

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (hitCollider == null)
                {
                    continue;
                }

                // Ignore this player's own colliders (body/eyes/mouth) and keep searching.
                if (hitCollider.transform.IsChildOf(transform))
                {
                    continue;
                }

                IInteractable interactable = hitCollider.GetComponentInParent<IInteractable>();
                if (interactable == null)
                {
                    continue;
                }

                interactable.Interact(gameObject);
                return;
            }
        }
    }
}
