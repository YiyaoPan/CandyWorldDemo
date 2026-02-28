// CameraFollow.cs
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Target")]
    public Transform target; // Drag your character Transform here

    [Header("Camera Position Settings")]
    [Tooltip("Distance directly behind the character (negative Z-axis)")]
    public float followDistance = 8f; // Larger for big maps, e.g., 10-15
    [Tooltip("Camera height (Y-axis)")]
    public float cameraHeight = 3f;   // Third-person view height
    [Tooltip("Camera follow smoothness")]
    public float smoothSpeed = 0.125f; // Larger = more responsive, smaller = smoother

    // Record current rotation angle around target's Y-axis
    private float currentRotationY = 0f;

    void LateUpdate()
    {
        if (target == null)
        {
            // Debug.LogError("Target not set for camera follow!", this);
            return;
        }

        // Fix: camera rotates around character's Y-axis, no longer fixed directly behind
        Vector3 desiredPosition = CalculateDesiredPosition();
        
        // Smooth camera movement (avoids jitter, suitable for large maps)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // Camera always looks at character's chest level (more natural, not at feet)
        transform.LookAt(target.position + new Vector3(0, 1f, 0));
    }

    /// <summary>
    /// Calculate camera's target position based on current rotation angle
    /// </summary>
    private Vector3 CalculateDesiredPosition()
    {
        // Calculate offset around character based on rotation angle
        Vector3 offset = new Vector3(0, cameraHeight, -followDistance);
        offset = Quaternion.Euler(0, currentRotationY, 0) * offset;
        
        // Final camera position = character position + rotated offset
        return target.position + offset;
    }

    /// <summary>
    /// Rotation method called by PlayerController
    /// </summary>
    /// <param name="deltaRotation">Rotation increment (degrees)</param>
    public void RotateAroundTarget(float deltaRotation)
    {
        if (target == null) return;
        
        // Update rotation angle
        currentRotationY += deltaRotation;
        // Optional: clamp rotation angle (e.g., 0-360 to avoid large values)
        currentRotationY = Mathf.Repeat(currentRotationY, 360f);
    }

    // Optional: preview camera position in scene view (for debugging)
    void OnDrawGizmos()
    {
        if (target != null)
        {
            // Draw character position
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(target.position, 0.5f);
            
            // Draw camera target position
            Gizmos.color = Color.blue;
            Vector3 previewPos = CalculateDesiredPosition();
            Gizmos.DrawSphere(previewPos, 0.3f);
            
            // Draw line from camera to character
            Gizmos.color = Color.green;
            Gizmos.DrawLine(previewPos, target.position + new Vector3(0, 1f, 0));
        }
    }
}