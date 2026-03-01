// CameraFollow.cs
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Target")]
    public Transform target;

    [Header("Camera Position Settings")]
    [Tooltip("Distance behind the character (negative Z direction)")]
    public float followDistance = 8f;
    [Tooltip("Camera height (Y axis)")]
    public float cameraHeight = 3f;
    [Tooltip("Camera follow smoothness")]
    public float smoothSpeed = 0.125f;

    private float currentRotationY = 0f;

    void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = CalculateDesiredPosition();
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        transform.LookAt(target.position + new Vector3(0, 1f, 0));
    }

    private Vector3 CalculateDesiredPosition()
    {
        Vector3 offset = new Vector3(0, cameraHeight, -followDistance);
        offset = Quaternion.Euler(0, currentRotationY, 0) * offset;
        return target.position + offset;
    }

    public void RotateAroundTarget(float deltaRotation)
    {
        if (target == null) return;
        currentRotationY += deltaRotation;
        currentRotationY = Mathf.Repeat(currentRotationY, 360f);
    }

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