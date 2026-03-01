// EnemyController.cs
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Basic Attributes")]
    public float maxHealth = 100f;
    private float currentHealth;
    public float moveSpeed = 3f;
    public float chaseRange = 8f;
    public float attackRange = 2f;
    public float attackCooldown = 2f;
    public int attackDamage = 20;
    public float deathDestroyDelay = 2f;

    [Header("Wander Settings")]
    public float wanderRadius = 10f;
    public float wanderInterval = 3f;
    public float wanderSpeed = 2f;

    [Header("References")]
    public Transform player;
    public Animator animator;
    public Transform attackPoint;
    public HealthBar3D healthBar;
    private Rigidbody rb;

    [Header("Debug")]
    public float rotationSpeed = 5f;
    public float stopDistance = 0.1f;

    private float lastAttackTime;
    private enum State { Idle, Chase, Attack, Death }
    private State currentState = State.Idle;

    private Vector3 wanderTarget;
    private float nextWanderTime;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    void Start()
    {
        currentHealth = maxHealth;

        if (healthBar != null)
        {
            Debug.Log($"[{gameObject.name}] Initializing health bar, max health: {maxHealth}, current health: {currentHealth}", this);
            healthBar.InitializeHealth(maxHealth, currentHealth, transform);
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] healthBar reference not assigned! Health bar cannot be displayed", this);
        }

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Enemy missing Rigidbody component! Please add and set to non-kinematic", this);
            enabled = false;
            return;
        }
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.freezeRotation = true;

        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
                Debug.LogWarning("Enemy missing Animator component! Animation control will be disabled", this);
        }

        if (attackPoint == null)
        {
            attackPoint = transform;
        }

        lastAttackTime = -attackCooldown;
    }

    void Update()
    {
        if (currentState == State.Death) return;

        if (currentHealth <= 0)
        {
            ChangeState(State.Death);
            return;
        }

        if (player == null)
        {
            Wander();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Idle:
                if (distanceToPlayer < chaseRange)
                    ChangeState(State.Chase);
                else
                    Wander();
                break;
            case State.Chase:
                if (distanceToPlayer > chaseRange)
                    ChangeState(State.Idle);
                else if (distanceToPlayer <= attackRange)
                    ChangeState(State.Attack);
                else
                    ChasePlayer();
                break;
            case State.Attack:
                if (distanceToPlayer > attackRange)
                    ChangeState(State.Chase);
                else if (Time.time > lastAttackTime + attackCooldown)
                {
                    AttackPlayer();
                    lastAttackTime = Time.time;
                }
                else
                    RotateToPlayer();
                break;
        }
    }

    void ChangeState(State newState)
    {
        if (currentState == newState) return;
        currentState = newState;

        if (animator == null) return;

        switch (newState)
        {
            case State.Idle:
                animator.SetBool("IsIdle", true);
                animator.SetBool("IsChasing", false);
                animator.SetBool("IsAttacking", false);
                StopMovement();
                break;
            case State.Chase:
                animator.SetBool("IsIdle", false);
                animator.SetBool("IsChasing", true);
                animator.SetBool("IsAttacking", false);
                break;
            case State.Attack:
                animator.SetBool("IsIdle", false);
                animator.SetBool("IsChasing", false);
                animator.SetBool("IsAttacking", true);
                StopMovement();
                break;
            case State.Death:
                animator.SetTrigger("Death");
                StopMovement();
                Collider col = GetComponent<Collider>();
                if (col) col.enabled = false;
                rb.isKinematic = true;
                enabled = false;
                Invoke(nameof(DestroyEnemy), deathDestroyDelay);
                break;
        }
    }

    void ChasePlayer()
    {
        if (player == null) return;
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        if (dir.magnitude > stopDistance)
        {
            RotateToPlayer();
            MoveWithRigidbody(dir, moveSpeed);
        }
        else StopMovement();
    }

    void Wander()
    {
        if (Time.time > nextWanderTime)
        {
            Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
            randomDir.y = 0;
            wanderTarget = transform.position + randomDir;
            nextWanderTime = Time.time + wanderInterval;
        }

        Vector3 dir = (wanderTarget - transform.position).normalized;
        if (dir.magnitude > 0.1f)
        {
            RotateTowards(dir);
            MoveWithRigidbody(dir, wanderSpeed);
        }
        else
        {
            StopMovement();
        }
    }

    void RotateTowards(Vector3 direction)
    {
        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    void MoveWithRigidbody(Vector3 direction, float speed)
    {
        Vector3 targetVel = direction * speed;
        targetVel.y = rb.velocity.y;
        rb.velocity = Vector3.Lerp(rb.velocity, targetVel, Time.deltaTime * 10f);
    }

    void StopMovement()
    {
        rb.velocity = new Vector3(0, rb.velocity.y, 0);
    }

    void RotateToPlayer()
    {
        if (player == null) return;
        Vector3 dir = player.position - transform.position;
        dir.y = 0;
        if (dir.magnitude > stopDistance)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    void AttackPlayer()
    {
        RotateToPlayer();
        if (animator != null)
        {
            animator.Play("Idle", 0, 0f);
            Invoke(nameof(ReplayAttackAnimation), 0.05f);
        }

        if (attackPoint == null) return;
        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Debug.Log($"[{gameObject.name}] Attack hit player! Damage: {attackDamage}", this);
                PlayerController pc = hit.GetComponent<PlayerController>();
                if (pc != null) pc.TakeDamage(attackDamage);
                break;
            }
        }
    }

    void ReplayAttackAnimation()
    {
        if (animator != null && currentState == State.Attack)
            animator.Play("Attack", 0, 0f);
    }

    public void TakeDamage(float damage)
    {
        if (currentState == State.Death) return;
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        Debug.Log($"[{gameObject.name}] Took damage: {damage}, remaining health: {currentHealth}", this);

        if (healthBar != null)
            healthBar.UpdateHealthDisplay(currentHealth);
        else
            Debug.LogError($"[{gameObject.name}] healthBar reference is null, cannot update health bar!", this);

        if (currentHealth > 0)
        {
            // Can play hit animation here
        }
        else
            ChangeState(State.Death);
    }

    // Initialization method called by GameManager
    public void InitializeForGame(float health, float speed, Transform playerTarget)
    {
        maxHealth = health;
        currentHealth = health;
        moveSpeed = speed;
        player = playerTarget;

        if (healthBar != null)
            healthBar.InitializeHealth(maxHealth, currentHealth, transform);
    }

    private void DestroyEnemy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnEnemyKilled(this);

        if (healthBar != null)
            Destroy(healthBar.gameObject);
        Destroy(gameObject);
    }

    public void OnDeathAnimationComplete()
    {
        CancelInvoke(nameof(DestroyEnemy));
        DestroyEnemy();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = Color.red;
        if (attackPoint != null)
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        else
            Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}