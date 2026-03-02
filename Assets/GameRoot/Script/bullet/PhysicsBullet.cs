// ====================================================
// PhysicsBullet.cs
// ====================================================

using UnityEngine;

public class PhysicsBullet : MonoBehaviour
{
    [Header("Bullet Parameters")]
    public float bulletSpeed = 50f;          // Initial speed of the bullet
    public float lifeTime = 2f;              // Time before the bullet is auto-destroyed
    public LayerMask hitLayer;                // Layers that the bullet can hit
    public float damage = 20f;                // Damage dealt to enemies

    private Rigidbody rb;                     // Reference to the Rigidbody component

    void Awake()
    {
        rb = GetComponent<Rigidbody>();       // Get the Rigidbody attached to this bullet
        // Apply an impulse force to launch the bullet forward
        rb.AddForce(transform.forward * bulletSpeed, ForceMode.Impulse);
        // Schedule destruction after lifeTime seconds
        Destroy(gameObject, lifeTime);
    }

    // Called when the bullet collides with another collider
    void OnCollisionEnter(Collision collision)
    {
        // Check if the collided object is an enemy
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log($"Bullet hit enemy: {collision.gameObject.name}");

            // Attempt to get the EnemyController component from the hit enemy
            EnemyController enemyController = collision.gameObject.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                enemyController.TakeDamage(damage);   // Apply damage to the enemy
            }
            else
            {
                Debug.LogWarning("EnemyController script not found on the hit enemy object!", this);
            }

            // Apply a force to the enemy's Rigidbody if it exists and is not kinematic
            Rigidbody hitRb = collision.collider.GetComponent<Rigidbody>();
            if (hitRb != null && !hitRb.isKinematic)
            {
                hitRb.AddForce(transform.forward * 200f);   // Push the enemy
            }

            Destroy(gameObject);   // Destroy the bullet upon impact
        }
        // Check if the collided object is ground/stone (wall)
        else if (collision.gameObject.CompareTag("Ground_Stone"))
        {
            Debug.Log("Bullet hit wall");
            Destroy(gameObject);
        }
        else
        {
            Debug.Log($"Bullet hit other object: {collision.gameObject.name}");
            Destroy(gameObject);
        }
    }

    // Draw a small yellow wire sphere in the Editor to visualize the bullet's position
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.05f);
    }
}