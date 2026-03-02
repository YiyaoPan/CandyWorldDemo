// ====================================================
// CameraFollow.cs
// ====================================================

using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;                         // The target the camera follows (player)

    [Header("Camera Position Settings")]
    [Tooltip("Distance behind the character (negative Z)")]
    public float followDistance = 8f;                 // Distance behind target
    [Tooltip("Camera height (Y axis)")]
    public float cameraHeight = 3f;                   // Height above target
    [Tooltip("Camera smoothing speed")]
    public float smoothSpeed = 0.125f;                 // Smoothing factor for camera movement

    private float currentRotationY = 0f;               // Accumulated rotation around Y axis

    void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = CalculateDesiredPosition();    // Compute ideal position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;                    // Apply smoothed position

        // Make camera look at target (with a slight height offset)
        transform.LookAt(target.position + new Vector3(0, 1f, 0));
    }

    // Calculates the desired camera position based on current rotation and offsets
    private Vector3 CalculateDesiredPosition()
    {
        Vector3 offset = new Vector3(0, cameraHeight, -followDistance);
        offset = Quaternion.Euler(0, currentRotationY, 0) * offset; // Apply rotation
        return target.position + offset;
    }

    // Allows external scripts to rotate the camera around the target
    public void RotateAroundTarget(float deltaRotation)
    {
        if (target == null) return;

        currentRotationY += deltaRotation;
        currentRotationY = Mathf.Repeat(currentRotationY, 360f); // Keep within 0-360
    }

    // Draw gizmos in the Editor to visualize target and camera position
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