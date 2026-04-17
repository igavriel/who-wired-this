using UnityEngine;

namespace ThirdPersonMixamo
{
    [RequireComponent(typeof(PlayerController))]
    public class ThirdPersonAnimatorBridge : MonoBehaviour
    {
        [SerializeField] private float speedSmoothing = 10f;

        private PlayerController _player;
        private CharacterController _controller;
        private Animator _animator;
        private float _speedBlend;
        private bool _jumpLatch;

        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Grounded = Animator.StringToHash("Grounded");
        private static readonly int Jump = Animator.StringToHash("Jump");
        private static readonly int FreeFall = Animator.StringToHash("FreeFall");
        private static readonly int MotionSpeed = Animator.StringToHash("MotionSpeed");

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _controller = _player.CharacterController;
            _animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            if (_player != null)
            {
                _player.JumpStarted += OnJumpStarted;
            }
        }

        private void OnDisable()
        {
            if (_player != null)
            {
                _player.JumpStarted -= OnJumpStarted;
            }
        }

        private void OnJumpStarted()
        {
            _jumpLatch = true;
        }

        private void LateUpdate()
        {
            if (_animator == null || _controller == null)
            {
                return;
            }

            Vector3 horizontal = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z);
            float horizontalSpeed = horizontal.magnitude;
            _speedBlend = Mathf.Lerp(_speedBlend, horizontalSpeed, Time.deltaTime * speedSmoothing);

            bool grounded = _controller.isGrounded;
            _animator.SetFloat(Speed, _speedBlend);
            _animator.SetBool(Grounded, grounded);
            _animator.SetFloat(MotionSpeed, horizontalSpeed > 0.05f ? 1f : 0f);
            _animator.SetBool(FreeFall, !grounded && _controller.velocity.y < -0.1f);

            if (_jumpLatch)
            {
                _animator.SetBool(Jump, true);
                _jumpLatch = false;
            }
            else
            {
                _animator.SetBool(Jump, false);
            }
        }
    }
}
