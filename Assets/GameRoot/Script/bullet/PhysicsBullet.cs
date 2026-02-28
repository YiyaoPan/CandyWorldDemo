// PhysicsBullet.cs
using UnityEngine;

public class PhysicsBullet : MonoBehaviour
{
    [Header("Bullet Parameters")]
    public float bulletSpeed = 50f; // Bullet flight speed (faster than particle version, more physical)
    public float lifeTime = 2f;     // Maximum bullet lifetime (prevents flying too far)
    public LayerMask hitLayer;      // Layers that can be hit (EnvMountains/Default)

    private Rigidbody rb;

    void Awake()
    {
        // Get the bullet's own Rigidbody component
        rb = GetComponent<Rigidbody>();
        // Add force to the Rigidbody to make the bullet fly forward along its own Z-axis
        rb.AddForce(transform.forward * bulletSpeed, ForceMode.Impulse);
        // Automatically destroy the bullet (prevents memory leaks)
        Destroy(gameObject, lifeTime);
    }

    // Physics collision listener (triggered when the bullet hits an object)
    void OnCollisionEnter(Collision collision)
    {
        // Check if the hit object has the "Enemy" tag
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log($"Bullet hit enemy: {collision.gameObject.name}");

            // Get the enemy controller and call the damage method
            EnemyController enemyController = collision.gameObject.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                // Adjust damage value as needed, e.g., 20 points
                enemyController.TakeDamage(20);
            }
            else
            {
                Debug.LogWarning("The enemy object hit does not have an EnemyController script!", this);
            }

            // Optional: Add force to the hit object (if it has a Rigidbody)
            Rigidbody hitRb = collision.collider.GetComponent<Rigidbody>();
            if (hitRb != null && !hitRb.isKinematic)
            {
                hitRb.AddForce(transform.forward * 200f);
            }

            // Destroy the bullet immediately after hitting
            Destroy(gameObject);
        }
    }

    // Draw the bullet collider in the scene view (for debugging)
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.05f);
    }
}