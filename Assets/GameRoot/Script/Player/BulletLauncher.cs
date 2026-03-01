// BulletLauncher.cs
using UnityEngine;

public class BulletLauncher : MonoBehaviour
{
    [Header("Launch Settings")]
    public GameObject physicsBulletPrefab;
    public Transform firePoint;
    public LayerMask hitLayer;

    [Header("Launch Angle (Debug)")]
    public float horizontalAngleOffset = 0f;
    public float verticalAngleOffset = 0f;

    [Header("Fire Position Offset")]
    public Vector3 firePositionOffset = new Vector3(0f, 0.5f, 0f);

    void Update() { }

    public void LaunchBullet(float damage)
    {
        if (physicsBulletPrefab == null || firePoint == null)
        {
            Debug.LogError("Please assign bullet prefab and fire point!");
            return;
        }

        Vector3 fireDirection = CalculateFireDirection();
        Vector3 finalFirePosition = firePoint.TransformPoint(firePositionOffset);

        GameObject bullet = Instantiate(physicsBulletPrefab, finalFirePosition, Quaternion.LookRotation(fireDirection));

        PhysicsBullet bulletScript = bullet.GetComponent<PhysicsBullet>();
        if (bulletScript != null)
        {
            bulletScript.hitLayer = hitLayer;
            bulletScript.damage = damage;
        }
    }

    private Vector3 CalculateFireDirection()
    {
        Vector3 baseDirection = firePoint.forward;
        Quaternion angleOffset = Quaternion.Euler(verticalAngleOffset, horizontalAngleOffset, 0);
        return (angleOffset * baseDirection).normalized;
    }

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