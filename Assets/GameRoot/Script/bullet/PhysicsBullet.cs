// PhysicsBullet.cs
using UnityEngine;

public class PhysicsBullet : MonoBehaviour
{
    [Header("Bullet Parameters")]
    public float bulletSpeed = 50f;
    public float lifeTime = 2f;
    public LayerMask hitLayer;
    public float damage = 20f;   // Damage value of the bullet

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.AddForce(transform.forward * bulletSpeed, ForceMode.Impulse);
        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log($"Bullet hit enemy: {collision.gameObject.name}");

            EnemyController enemyController = collision.gameObject.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                enemyController.TakeDamage(damage);
            }
            else
            {
                Debug.LogWarning("Enemy object does not have EnemyController script!", this);
            }

            Rigidbody hitRb = collision.collider.GetComponent<Rigidbody>();
            if (hitRb != null && !hitRb.isKinematic)
            {
                hitRb.AddForce(transform.forward * 200f);
            }

            Destroy(gameObject);
        }
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

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.05f);
    }
}