// ====================================================
// BulletLauncher.cs
// ====================================================

using UnityEngine;

public class BulletLauncher : MonoBehaviour
{
    [Header("Launch Settings")]
    public GameObject physicsBulletPrefab;       // Prefab of the bullet to instantiate
    public Transform firePoint;                   // Point from which bullets are fired
    public LayerMask hitLayer;                     // Layer mask to pass to bullet (for collision)

    [Header("Angle Offsets (Debug)")]
    public float horizontalAngleOffset = 0f;       // Horizontal angle offset (degrees)
    public float verticalAngleOffset = 0f;         // Vertical angle offset (degrees)

    [Header("Fire Position Offset")]
    public Vector3 firePositionOffset = new Vector3(0f, 0.5f, 0f); // Local offset from firePoint

    void Update() { }  // Empty Update (could be removed, but kept for potential future use)

    // Launches a bullet with the specified damage
    public void LaunchBullet(float damage)
    {
        if (physicsBulletPrefab == null || firePoint == null)
        {
            Debug.LogError("Please assign bullet prefab and fire point!");
            return;
        }

        Vector3 fireDirection = CalculateFireDirection();                     // Direction with offsets applied
        Vector3 finalFirePosition = firePoint.TransformPoint(firePositionOffset); // World position with offset

        // Instantiate the bullet with rotation matching the fire direction
        GameObject bullet = Instantiate(physicsBulletPrefab, finalFirePosition, Quaternion.LookRotation(fireDirection));

        PhysicsBullet bulletScript = bullet.GetComponent<PhysicsBullet>();
        if (bulletScript != null)
        {
            bulletScript.hitLayer = hitLayer;   // Pass hit layer to bullet
            bulletScript.damage = damage;       // Set bullet damage
        }
    }

    // Calculates the fire direction by applying angle offsets to the base forward direction
    private Vector3 CalculateFireDirection()
    {
        Vector3 baseDirection = firePoint.forward;
        Quaternion angleOffset = Quaternion.Euler(verticalAngleOffset, horizontalAngleOffset, 0);
        return (angleOffset * baseDirection).normalized;
    }

    // Draw gizmos in the Editor to visualize fire point and direction
    void OnDrawGizmosSelected()
    {
        if (firePoint != null)
        {
            Vector3 finalFirePosition = firePoint.TransformPoint(firePositionOffset);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(firePoint.position, 0.1f);
            Gizmos.DrawLine(firePoint.position, firePoint.position + firePoint.forward * 50f);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(finalFirePosition, 0.1f);
            Vector3 debugDirection = CalculateFireDirection();
            Gizmos.DrawLine(finalFirePosition, finalFirePosition + debugDirection * 50f);
        }
    }
}