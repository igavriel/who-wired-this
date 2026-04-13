using UnityEngine;

public class CameraFollowAbove : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = new Vector3(0, 10, 0); // Default offset (adjustable in the Inspector)
    public float smoothSpeed = 0.125f; // Default smooth speed (adjustable in the Inspector)

    void LateUpdate()
    {
        // Calculate the camera's desired position relative to the player
        Vector3 desiredPosition = new Vector3(player.position.x, 0, player.position.z) + offset;

        // Smoothly interpolate between current and desired positions
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Update the camera's position
        transform.position = smoothedPosition;
    }
}
