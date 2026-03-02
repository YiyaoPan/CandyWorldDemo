using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Target")]
    public Transform target; // Drag your character Transform here

    [Header("Camera Position Settings")]
    [Tooltip("Distance directly behind the character (negative Z axis)")]
    public float followDistance = 8f; // For large maps, increase to 10-15
    [Tooltip("Camera height (Y axis)")]
    public float cameraHeight = 3f;   // Third-person perspective height
    [Tooltip("Camera follow smoothness")]
    public float smoothSpeed = 0.125f; // Larger = more responsive, smaller = smoother

    // Record current rotation angle (around target's Y axis)
    private float currentRotationY = 0f;

    void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        // Calculate desired position based on current rotation
        Vector3 desiredPosition = CalculateDesiredPosition();
        
        // Smoothly move camera
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // Camera always looks at character's chest (more natural)
        transform.LookAt(target.position + new Vector3(0, 1f, 0));
    }

    /// <summary>
    /// Calculate camera's target position based on current rotation angle
    /// </summary>
    private Vector3 CalculateDesiredPosition()
    {
        Vector3 offset = new Vector3(0, cameraHeight, -followDistance);
        offset = Quaternion.Euler(0, currentRotationY, 0) * offset;
        return target.position + offset;
    }

    /// <summary>
    /// Rotation method called by PlayerController
    /// </summary>
    /// <param name="deltaRotation">Rotation increment (degrees)</param>
    public void RotateAroundTarget(float deltaRotation)
    {
        if (target == null) return;
        currentRotationY += deltaRotation;
        currentRotationY = Mathf.Repeat(currentRotationY, 360f);
    }

    // Preview camera position in Scene view (for debugging)
    void OnDrawGizmos()
    {
        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(target.position, 0.5f);
            
            Gizmos.color = Color.blue;
            Vector3 previewPos = CalculateDesiredPosition();
            Gizmos.DrawSphere(previewPos, 0.3f);
            
            Gizmos.color = Color.green;
            Gizmos.DrawLine(previewPos, target.position + new Vector3(0, 1f, 0));
        }
    }
}