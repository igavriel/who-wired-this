using UnityEngine;

namespace ThirdPersonMixamo
{
    public class PlayerCameraRig : MonoBehaviour
    {
        [Header("Follow")]
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 6f;
        [SerializeField] private float height = 3f;
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.2f, 0f);
        [SerializeField] private bool lockYawOnStart = true;

        [Header("Cinematic Occlusion")]
        [SerializeField] private bool cinematicModeEnabled = true;
        [SerializeField] private LayerMask occluderMask = ~0;
        [SerializeField] private float occlusionProbeRadius = 0.2f;
        [SerializeField] private float blockedHeight = 7f;
        [SerializeField] private float blockedDistance = 1.5f;

        private float _lockedYaw;
        private bool _isInitialized;

        private void Start()
        {
            if (!lockYawOnStart)
            {
                return;
            }

            _lockedYaw = transform.eulerAngles.y;
            _isInitialized = true;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (lockYawOnStart && !_isInitialized)
            {
                _lockedYaw = transform.eulerAngles.y;
                _isInitialized = true;
            }

            Quaternion yawRotation = Quaternion.Euler(0f, lockYawOnStart ? _lockedYaw : transform.eulerAngles.y, 0f);
            Vector3 followOffset = yawRotation * new Vector3(0f, height, -distance);
            Vector3 desiredPosition = target.position + followOffset;
            Vector3 lookTarget = target.position + lookOffset;

            if (cinematicModeEnabled && IsPlayerOccluded(lookTarget, desiredPosition, out _))
            {
                Vector3 overheadOffset = yawRotation * new Vector3(0f, blockedHeight, -blockedDistance);
                desiredPosition = target.position + overheadOffset;
            }

            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
        }

        public void SetCinematicMode(bool isEnabled)
        {
            cinematicModeEnabled = isEnabled;
        }

        private bool IsPlayerOccluded(Vector3 lookTarget, Vector3 desiredPosition, out RaycastHit hit)
        {
            Vector3 direction = desiredPosition - lookTarget;
            float distanceToCamera = direction.magnitude;

            if (distanceToCamera <= 0.001f)
            {
                hit = default;
                return false;
            }

            if (!Physics.SphereCast(
                    lookTarget,
                    occlusionProbeRadius,
                    direction.normalized,
                    out hit,
                    distanceToCamera,
                    occluderMask,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            // Ignore hits on the player itself so only world blockers trigger overhead mode.
            if (hit.transform != null && hit.transform.IsChildOf(target))
            {
                return false;
            }

            return true;
        }
    }
}
