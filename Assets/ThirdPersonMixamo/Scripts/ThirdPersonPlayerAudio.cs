using UnityEngine;

namespace ThirdPersonMixamo
{
    [RequireComponent(typeof(PlayerController))]
    public class ThirdPersonPlayerAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource footstepSource;
        [SerializeField] private AudioClip footstepClip;
        [SerializeField] private AudioClip landClip;
        [SerializeField] private float footstepIntervalWalk = 0.45f;
        [SerializeField] private float footstepIntervalSprint = 0.32f;
        [SerializeField] private float minSpeedForFootsteps = 0.35f;

        private PlayerController _player;
        private CharacterController _controller;
        private float _footstepTimer;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _controller = _player.CharacterController;
            if (footstepSource == null)
            {
                footstepSource = gameObject.AddComponent<AudioSource>();
                footstepSource.spatialBlend = 1f;
                footstepSource.minDistance = 1f;
                footstepSource.maxDistance = 25f;
            }
        }

        private void OnEnable()
        {
            _player.Landed += OnLanded;
        }

        private void OnDisable()
        {
            _player.Landed -= OnLanded;
        }

        private void OnLanded()
        {
            if (landClip != null && footstepSource != null)
            {
                footstepSource.PlayOneShot(landClip);
            }
        }

        private void Update()
        {
            if (footstepClip == null || _controller == null)
            {
                return;
            }

            Vector3 horizontal = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z);
            float speed = horizontal.magnitude;
            if (!_controller.isGrounded || speed < minSpeedForFootsteps)
            {
                return;
            }

            float interval = speed > 5f ? footstepIntervalSprint : footstepIntervalWalk;
            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer <= 0f)
            {
                _footstepTimer = interval;
                footstepSource.PlayOneShot(footstepClip);
            }
        }
    }
}
