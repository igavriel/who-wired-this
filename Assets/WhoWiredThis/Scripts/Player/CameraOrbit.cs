using UnityEngine;

namespace WhoWiredThis.Player
{
    public class CameraOrbit : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;
        public Vector3 offset = new Vector3(0f, 2f, -5f);

        [Header("Orbit")]
        public float mouseSensitivity = 3f;
        public float minPitch = -20f;
        public float maxPitch = 60f;

        private float yaw;
        private float pitch = 20f;

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            yaw   += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = target.position + rotation * offset;
            transform.LookAt(target.position + Vector3.up * 1f);
        }
    }
}
