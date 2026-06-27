using UnityEngine;

namespace WhoWiredThis.PanelFocus
{
    public enum PanelFocusViewAxis
    {
        Forward = 0,
        Back = 1,
        Up = 2,
        Down = 3,
        Right = 4,
        Left = 5
    }

    /// <summary>
    /// Camera framing for panel focus. Lives on the same GameObject as
    /// <see cref="PanelFocusController"/> (typically Board) and supplies
    /// <see cref="GetCameraSnapPose"/> for startup and interact focus.
    /// </summary>
    public class PanelFocusCamera : MonoBehaviour
    {
        [Header("Camera Framing")]
        [Tooltip("Percent of the screen height/width occupied by the full board frame while focused.")]
        [Range(10f, 100f)]
        [SerializeField]
        private float frameFillPercent = 95f;

        [Tooltip("Optional board renderer used for framing. If empty, uses Renderer on this GameObject.")]
        [SerializeField]
        private Renderer boardRenderer;

        [Tooltip("Optional orientation for camera snap. When empty, uses the board renderer transform, then this object.")]
        [SerializeField]
        private Transform framingTransform;

        [Tooltip("Local axis on the framing transform that points from the camera toward the board face.")]
        [SerializeField]
        private PanelFocusViewAxis viewAxis = PanelFocusViewAxis.Forward;

        [Tooltip("Small safety offset added to the computed camera distance.")]
        [SerializeField]
        private float extraDistance = 0.02f;

        public void GetCameraSnapPose(Camera playerCamera, out Vector3 worldPos, out Quaternion worldRot)
        {
            Renderer targetRenderer = boardRenderer != null ? boardRenderer : GetComponent<Renderer>();
            Transform framing = ResolveFramingTransform(targetRenderer);
            Vector3 viewDirection = GetViewDirection(framing);
            float fill = Mathf.Clamp(frameFillPercent / 100f, 0.1f, 1f);
            Quaternion boardRotation = Quaternion.LookRotation(viewDirection);
            Vector3 boardCenter = targetRenderer != null ? targetRenderer.bounds.center : framing.position;

            if (playerCamera == null || targetRenderer == null)
            {
                worldRot = boardRotation;
                worldPos = boardCenter - viewDirection * (1f + Mathf.Max(0f, extraDistance));
                return;
            }

            Vector3 localExtents = framing.InverseTransformVector(targetRenderer.bounds.extents);
            float halfWidth = Mathf.Abs(localExtents.x);
            float halfHeight = Mathf.Abs(localExtents.y);

            if (viewAxis != PanelFocusViewAxis.Forward || framing != transform)
            {
                GetViewPlaneExtents(targetRenderer.bounds, viewDirection, out halfWidth, out halfHeight);
            }

            float verticalHalfFovRad = 0.5f * playerCamera.fieldOfView * Mathf.Deg2Rad;
            float horizontalHalfFovRad = Mathf.Atan(Mathf.Tan(verticalHalfFovRad) * playerCamera.aspect);

            float distanceForHeight = halfHeight / (Mathf.Tan(verticalHalfFovRad) * fill);
            float distanceForWidth = halfWidth / (Mathf.Tan(horizontalHalfFovRad) * fill);
            float distance = Mathf.Max(distanceForHeight, distanceForWidth) + Mathf.Max(0f, extraDistance);

            worldRot = boardRotation;
            worldPos = boardCenter - viewDirection * distance;
        }

        private static Vector3 GetViewDirection(Transform framing, PanelFocusViewAxis axis)
        {
            switch (axis)
            {
                case PanelFocusViewAxis.Back:
                    return -framing.forward;
                case PanelFocusViewAxis.Up:
                    return framing.up;
                case PanelFocusViewAxis.Down:
                    return -framing.up;
                case PanelFocusViewAxis.Right:
                    return framing.right;
                case PanelFocusViewAxis.Left:
                    return -framing.right;
                default:
                    return framing.forward;
            }
        }

        private Vector3 GetViewDirection(Transform framing)
        {
            return GetViewDirection(framing, viewAxis).normalized;
        }

        private static void GetViewPlaneExtents(Bounds bounds, Vector3 viewDirection, out float halfWidth, out float halfHeight)
        {
            viewDirection.Normalize();
            Vector3 right = Vector3.Cross(viewDirection, Vector3.up);
            if (right.sqrMagnitude < 0.001f)
            {
                right = Vector3.Cross(viewDirection, Vector3.forward);
            }

            right.Normalize();
            Vector3 up = Vector3.Cross(right, viewDirection).normalized;
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            halfWidth = 0f;
            halfHeight = 0f;
            for (int xi = -1; xi <= 1; xi += 2)
            {
                for (int yi = -1; yi <= 1; yi += 2)
                {
                    for (int zi = -1; zi <= 1; zi += 2)
                    {
                        Vector3 corner = center + Vector3.Scale(extents, new Vector3(xi, yi, zi));
                        Vector3 offset = corner - center;
                        halfWidth = Mathf.Max(halfWidth, Mathf.Abs(Vector3.Dot(offset, right)));
                        halfHeight = Mathf.Max(halfHeight, Mathf.Abs(Vector3.Dot(offset, up)));
                    }
                }
            }
        }

        private Transform ResolveFramingTransform(Renderer targetRenderer)
        {
            if (framingTransform != null)
            {
                return framingTransform;
            }

            if (targetRenderer != null)
            {
                return targetRenderer.transform;
            }

            return transform;
        }
    }
}
