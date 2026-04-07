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
            // Free cursor so the mouse can interact with UI and buttons.
            // Hold Right Mouse Button to orbit the camera.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            // Only orbit while right mouse button is held — keeps cursor free for UI otherwise.
            if (Input.GetMouseButton(1))
            {
                yaw   += Input.GetAxis("Mouse X") * mouseSensitivity;
                pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
                pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = target.position + rotation * offset;
            transform.LookAt(target.position + Vector3.up * 1f);
        }
    }
}
