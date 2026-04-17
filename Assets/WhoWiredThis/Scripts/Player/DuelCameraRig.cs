using UnityEngine;

namespace WhoWiredThis.Player
{
    public class DuelCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 6f;
        [SerializeField] private float height = 3f;
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.2f, 0f);
        [SerializeField] private bool lockYawOnStart = true;

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
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

            Vector3 lookTarget = target.position + lookOffset;
            transform.rotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
        }
    }
}
