// BulletLauncher.cs
using UnityEngine;

public class BulletLauncher : MonoBehaviour
{
    [Header("Launch Settings")]
    public GameObject physicsBulletPrefab; // Drag the bullet prefab here
    public Transform firePoint;            // Fire point (Z-axis forward)
    public LayerMask hitLayer;             // Layers that can be hit

    [Header("Launch Angle Settings (Debug)")]
    [Tooltip("Horizontal angle offset (left/right), 0 = straight ahead, positive = right, negative = left")]
    public float horizontalAngleOffset = 0f; // Horizontal offset in degrees
    [Tooltip("Vertical angle offset (up/down), 0 = straight ahead, positive = up, negative = down")]
    public float verticalAngleOffset = 0f;   // Vertical offset in degrees

    [Header("Fire Position Offset (Fix up/down/left/right offset issues)")]
    [Tooltip("Fire point position offset (local coordinates), adjust Y to fix bullet too low")]
    public Vector3 firePositionOffset = new Vector3(0f, 0.5f, 0f); // Default up 0.5 units

    void Update()
    {
        // Keep empty for external call interface
    }

    public void LaunchBullet()
    {
        if (physicsBulletPrefab == null || firePoint == null)
        {
            Debug.LogError("Please assign bullet prefab and fire point!");
            return;
        }

        // 1. Calculate final fire direction (based on configured angles)
        Vector3 fireDirection = CalculateFireDirection();

        // 2. Calculate final fire position (fire point position + local offset transformed to world)
        Vector3 finalFirePosition = firePoint.TransformPoint(firePositionOffset);
        
        // 3. Instantiate bullet (using offset position, rotation towards fire direction)
        GameObject bullet = Instantiate(physicsBulletPrefab, finalFirePosition, Quaternion.LookRotation(fireDirection));
        
        // 4. Assign hit layer to bullet
        PhysicsBullet bulletScript = bullet.GetComponent<PhysicsBullet>();
        if (bulletScript != null)
        {
            bulletScript.hitLayer = hitLayer;
        }
    }

    /// <summary>
    /// Calculate fire direction with angle offset (random spread removed)
    /// </summary>
    private Vector3 CalculateFireDirection()
    {
        // Base direction: forward of fire point (Z-axis)
        Vector3 baseDirection = firePoint.forward;

        // Apply horizontal/vertical angle offsets
        // Rotate around Y axis = horizontal offset (left/right), around X axis = vertical offset (up/down)
        Quaternion angleOffset = Quaternion.Euler(verticalAngleOffset, horizontalAngleOffset, 0);
        Vector3 offsetDirection = angleOffset * baseDirection;

        return offsetDirection.normalized; // Normalize to ensure direction vector length is 1
    }

    // Draw in scene view: original direction + actual fire direction + offset fire point (for debugging)
    void OnDrawGizmosSelected()
    {
        if (firePoint != null)
        {
            // Calculate offset fire position
            Vector3 finalFirePosition = firePoint.TransformPoint(firePositionOffset);
            
            // Red: original fire point and forward (Z-axis)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(firePoint.position, 0.1f); // Original fire point sphere
            Gizmos.DrawLine(firePoint.position, firePoint.position + firePoint.forward * 50f);

            // Green: offset fire point and actual fire direction
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(finalFirePosition, 0.1f); // Offset fire point sphere
            Vector3 debugDirection = CalculateFireDirection();
            Gizmos.DrawLine(finalFirePosition, finalFirePosition + debugDirection * 50f);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if hit target layer
        if (((1 << collision.gameObject.layer) & hitLayer) != 0)
        {
            // Debug.Log($"Bullet hit: {collision.gameObject.name}");
            
            // // Reduce enemy health
            // EnemyController enemy = collision.collider.GetComponent<EnemyController>();
            // if (enemy != null)
            // {
            //     enemy.TakeDamage(25f); // 25 damage per bullet
            // }

            // // Optional: Add force to hit object
            // Rigidbody hitRb = collision.collider.GetComponent<Rigidbody>();
            // if (hitRb != null && !hitRb.isKinematic)
            // {
            //     hitRb.AddForce(transform.forward * 200f);
            // }

            // // Destroy bullet immediately after hit
            // Destroy(gameObject);
        }
    }
}