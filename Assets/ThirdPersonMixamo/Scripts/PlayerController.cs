using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonMixamo
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private PlayerControlBindings inputBindings;

        [Header("Movement")]
        [SerializeField] private float velocity = 5f;
        [SerializeField] private float sprintAdittion = 3.5f;
        [SerializeField] private float jumpForce = 18f;
        [SerializeField] private float jumpTime = 0.85f;
        [SerializeField] private float gravity = 9.8f;
        [SerializeField] private float animationBlendRate = 10f;

        private float _jumpElapsedTime;
        private bool _isJumping;
        private bool _isSprinting;
        private bool _jumpAnimatorLatch;
        private bool _wasGrounded = true;
        private float _animatorSpeedBlend;

        private float _inputHorizontal;
        private float _inputVertical;
        private bool _inputJump;
        private bool _inputSprint;
        private bool _inputInteract;

        private CharacterController _controller;
        private Animator _animator;
        private readonly HashSet<string> _animatorParameters = new HashSet<string>();

        public event Action JumpStarted;
        public event Action Landed;

        public CharacterController CharacterController => _controller;
        public bool IsGrounded => _controller != null && _controller.isGrounded;
        public Vector3 Velocity => _controller != null ? _controller.velocity : Vector3.zero;
        public float AnimatorSpeedBlend => _animatorSpeedBlend;
        public float AnimatorMotionSpeed { get; private set; }
        public bool InteractPressedThisFrame => _inputInteract;
        private KeyCode MoveForwardKey => inputBindings != null ? inputBindings.MoveForward : KeyCode.W;
        private KeyCode MoveBackKey => inputBindings != null ? inputBindings.MoveBack : KeyCode.S;
        private KeyCode MoveLeftKey => inputBindings != null ? inputBindings.MoveLeft : KeyCode.A;
        private KeyCode MoveRightKey => inputBindings != null ? inputBindings.MoveRight : KeyCode.D;
        private KeyCode SprintKey => inputBindings != null ? inputBindings.Sprint : KeyCode.LeftShift;
        private KeyCode JumpKey => inputBindings != null ? inputBindings.Jump : KeyCode.Space;
        private KeyCode InteractKey => inputBindings != null ? inputBindings.Interact : KeyCode.LeftControl;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>();

            if (_animator == null)
            {
                Debug.LogWarning("[ThirdPersonMixamo] PlayerController found no Animator under " + gameObject.name + ".");
                return;
            }

            CacheAnimatorParameters();
            EnsureAnimationEventReceiver();
        }

        private void Update()
        {
            _inputHorizontal = 0f;
            _inputVertical = 0f;
            if (Input.GetKey(MoveLeftKey)) _inputHorizontal -= 1f;
            if (Input.GetKey(MoveRightKey)) _inputHorizontal += 1f;
            if (Input.GetKey(MoveBackKey)) _inputVertical -= 1f;
            if (Input.GetKey(MoveForwardKey)) _inputVertical += 1f;

            _inputJump = Input.GetKeyDown(JumpKey);
            _inputSprint = Input.GetKey(SprintKey);
            _inputInteract = Input.GetKeyDown(InteractKey);
            bool hasMoveInput = Mathf.Abs(_inputHorizontal) > 0.01f || Mathf.Abs(_inputVertical) > 0.01f;
            _isSprinting = hasMoveInput && _inputSprint;

            if (_controller.isGrounded && _animator != null)
            {
                float targetAnimSpeed = hasMoveInput ? (_isSprinting ? velocity + sprintAdittion : velocity) : 0f;
                _animatorSpeedBlend = Mathf.Lerp(_animatorSpeedBlend, targetAnimSpeed, Time.deltaTime * animationBlendRate);
                if (_animatorSpeedBlend < 0.01f)
                {
                    _animatorSpeedBlend = 0f;
                }
                AnimatorMotionSpeed = hasMoveInput ? 1f : 0f;
                SetFloatIfExists("Speed", _animatorSpeedBlend);
                SetFloatIfExists("MotionSpeed", AnimatorMotionSpeed);
            }

            if (_animator != null)
            {
                bool isAir = !_controller.isGrounded;
                SetBoolIfExists("Grounded", !isAir);
                SetBoolIfExists("FreeFall", isAir && _controller.velocity.y < -0.1f);
                if (_jumpAnimatorLatch)
                {
                    SetBoolIfExists("Jump", true);
                    _jumpAnimatorLatch = false;
                }
                else
                {
                    SetBoolIfExists("Jump", false);
                }
            }

            if (_inputJump && _controller.isGrounded)
            {
                _isJumping = true;
                _jumpAnimatorLatch = true;
                JumpStarted?.Invoke();
            }

            HeadHittingDetect();
        }

        private void FixedUpdate()
        {
            bool groundedBeforeMove = _controller.isGrounded;
            float velocityAdittion = 0f;
            if (_isSprinting) velocityAdittion = sprintAdittion;

            float directionX = _inputHorizontal * (velocity + velocityAdittion) * Time.deltaTime;
            float directionZ = _inputVertical * (velocity + velocityAdittion) * Time.deltaTime;
            float directionY = 0f;

            if (_isJumping)
            {
                directionY = Mathf.SmoothStep(jumpForce, jumpForce * 0.30f, _jumpElapsedTime / jumpTime) * Time.deltaTime;
                _jumpElapsedTime += Time.deltaTime;
                if (_jumpElapsedTime >= jumpTime)
                {
                    _isJumping = false;
                    _jumpElapsedTime = 0f;
                }
            }

            directionY -= gravity * Time.deltaTime;

            Camera activeCamera = Camera.main;
            if (activeCamera == null)
            {
                _controller.Move(Vector3.up * directionY);
                return;
            }

            Vector3 forward = activeCamera.transform.forward;
            Vector3 right = activeCamera.transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            forward *= directionZ;
            right *= directionX;

            if (directionX != 0f || directionZ != 0f)
            {
                float angle = Mathf.Atan2(forward.x + right.x, forward.z + right.z) * Mathf.Rad2Deg;
                Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 0.15f);
            }

            Vector3 movement = Vector3.up * directionY + forward + right;
            _controller.Move(movement);

            bool groundedAfterMove = _controller.isGrounded;
            if (groundedAfterMove && !_wasGrounded)
            {
                Landed?.Invoke();
            }
            _wasGrounded = groundedAfterMove;
        }

        private void CacheAnimatorParameters()
        {
            _animatorParameters.Clear();
            foreach (AnimatorControllerParameter parameter in _animator.parameters)
            {
                _animatorParameters.Add(parameter.name);
            }
        }

        private void SetBoolIfExists(string parameterName, bool value)
        {
            if (_animator != null && _animatorParameters.Contains(parameterName))
            {
                _animator.SetBool(parameterName, value);
            }
        }

        private void SetFloatIfExists(string parameterName, float value)
        {
            if (_animator != null && _animatorParameters.Contains(parameterName))
            {
                _animator.SetFloat(parameterName, value);
            }
        }

        private void HeadHittingDetect()
        {
            float headHitDistance = 1.1f;
            Vector3 ccCenter = transform.TransformPoint(_controller.center);
            float hitCalc = _controller.height / 2f * headHitDistance;

            if (Physics.Raycast(ccCenter, Vector3.up, hitCalc))
            {
                _jumpElapsedTime = 0f;
                _isJumping = false;
            }
        }

        private void EnsureAnimationEventReceiver()
        {
            if (_animator == null)
            {
                return;
            }

            PlayerAnimationEventReceiver receiver = _animator.gameObject.GetComponent<PlayerAnimationEventReceiver>();
            if (receiver == null)
            {
                receiver = _animator.gameObject.AddComponent<PlayerAnimationEventReceiver>();
            }

            receiver.Initialize(this);
        }

        public void HandleFootstepAnimationEvent(AnimationEvent animationEvent)
        {
            // Intentionally empty: this consumes OnFootstep events so Animator does not log missing receiver errors.
        }

        public void HandleLandAnimationEvent(AnimationEvent animationEvent)
        {
            // Intentionally empty: some clips may emit OnLand.
        }
    }

    public class PlayerAnimationEventReceiver : MonoBehaviour
    {
        private PlayerController _owner;

        public void Initialize(PlayerController owner)
        {
            _owner = owner;
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            _owner?.HandleFootstepAnimationEvent(animationEvent);
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            _owner?.HandleLandAnimationEvent(animationEvent);
        }
    }
}
