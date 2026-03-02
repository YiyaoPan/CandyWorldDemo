// ====================================================
// EnemyController.cs
// ====================================================

using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Base Attributes")]
    public float maxHealth = 100f;             // Maximum health of the enemy
    private float currentHealth;                // Current health of the enemy
    public float moveSpeed = 3f;                // Movement speed when chasing
    public float chaseRange = 8f;                // Distance at which enemy starts chasing player
    public float attackRange = 2f;               // Distance at which enemy can attack player
    public float attackCooldown = 2f;            // Time between attacks
    public int attackDamage = 20;                // Damage dealt to player per attack
    public float deathDestroyDelay = 2f;         // Delay before enemy is destroyed after death

    [Header("Wander Settings")]
    public float wanderRadius = 10f;             // Radius for random wandering
    public float wanderInterval = 3f;            // Time between selecting new wander target
    public float wanderSpeed = 2f;                // Movement speed when wandering

    [Header("References")]
    public Transform player;                      // Reference to the player's transform
    public Animator animator;                     // Animator component for animations
    public Transform attackPoint;                 // Point from which attack range is checked
    public HealthBar3D healthBar;                  // Reference to the 3D health bar
    private Rigidbody rb;                          // Rigidbody component for physics movement

    [Header("Debug")]
    public float rotationSpeed = 5f;               // Speed of rotation towards target
    public float stopDistance = 0.1f;               // Distance threshold to stop moving

    private float lastAttackTime;                   // Time of last attack
    private enum State { Idle, Chase, Attack, Death }   // Possible enemy states
    private State currentState = State.Idle;        // Current state of the enemy

    private Vector3 wanderTarget;                   // Current wander destination
    private float nextWanderTime;                    // Time to pick new wander target

    public float CurrentHealth => currentHealth;     // Public getter for current health
    public float MaxHealth => maxHealth;             // Public getter for max health

    void Start()
    {
        currentHealth = maxHealth;                   // Initialize health

        // Initialize health bar if assigned
        if (healthBar != null)
        {
            Debug.Log($"[{gameObject.name}] Initializing health bar, max health: {maxHealth}, current health: {currentHealth}", this);
            healthBar.InitializeHealth(maxHealth, currentHealth, transform);
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] healthBar reference not assigned! Health bar cannot be displayed", this);
        }

        rb = GetComponent<Rigidbody>();               // Get Rigidbody component
        if (rb == null)
        {
            Debug.LogError("Enemy missing Rigidbody component! Please add and set to non-kinematic", this);
            enabled = false;                           // Disable this script if Rigidbody is missing
            return;
        }
        rb.isKinematic = false;                        // Ensure Rigidbody is not kinematic
        rb.useGravity = true;                           // Enable gravity
        rb.freezeRotation = true;                       // Prevent rotation from physics

        if (animator == null)
        {
            animator = GetComponent<Animator>();        // Try to get Animator if not assigned
            if (animator == null)
                Debug.LogWarning("Enemy missing Animator component! Animation control will be disabled", this);
        }

        if (attackPoint == null)
        {
            attackPoint = transform;                     // Default attack point to enemy's transform
        }

        lastAttackTime = -attackCooldown;                // Initialize so attack can happen immediately
    }

    void Update()
    {
        if (currentState == State.Death) return;         // Do nothing if dead

        // Check for death condition
        if (currentHealth <= 0)
        {
            ChangeState(State.Death);
            return;
        }

        // If player reference is missing, just wander
        if (player == null)
        {
            Wander();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // State machine logic
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
                    AttackPlayer();                         // Perform attack
                    lastAttackTime = Time.time;
                }
                else
                    RotateToPlayer();                        // Face player while waiting for cooldown
                break;
        }
    }

    // Changes the enemy's state and updates animator parameters accordingly
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
                StopMovement();                               // Stop moving when idle
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
                StopMovement();                               // Stop moving while attacking
                break;
            case State.Death:
                animator.SetTrigger("Death");
                StopMovement();
                Collider col = GetComponent<Collider>();
                if (col) col.enabled = false;                 // Disable collider so enemy can't be hit
                rb.isKinematic = true;                         // Make Rigidbody kinematic
                enabled = false;                                // Disable this script
                Invoke(nameof(DestroyEnemy), deathDestroyDelay); // Destroy after delay
                break;
        }
    }

    // Moves the enemy towards the player
    void ChasePlayer()
    {
        if (player == null) return;
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;                                             // Ignore vertical component
        if (dir.magnitude > stopDistance)                      // If not too close
        {
            RotateToPlayer();                                   // Face player
            MoveWithRigidbody(dir, moveSpeed);                  // Move using Rigidbody
        }
        else StopMovement();
    }

    // Wander behavior: picks random points within wanderRadius and moves there
    void Wander()
    {
        if (Time.time > nextWanderTime)                        // Time to pick new wander target
        {
            Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
            randomDir.y = 0;
            wanderTarget = transform.position + randomDir;
            nextWanderTime = Time.time + wanderInterval;
        }

        Vector3 dir = (wanderTarget - transform.position).normalized;
        if (dir.magnitude > 0.1f)                               // If not at target
        {
            RotateTowards(dir);                                  // Rotate towards target
            MoveWithRigidbody(dir, wanderSpeed);                 // Move
        }
        else
        {
            StopMovement();
        }
    }

    // Rotates towards a given direction smoothly
    void RotateTowards(Vector3 direction)
    {
        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    // Moves the enemy using Rigidbody velocity, preserving vertical velocity
    void MoveWithRigidbody(Vector3 direction, float speed)
    {
        Vector3 targetVel = direction * speed;
        targetVel.y = rb.velocity.y;                             // Keep current vertical velocity (gravity)
        rb.velocity = Vector3.Lerp(rb.velocity, targetVel, Time.deltaTime * 10f); // Smooth transition
    }

    // Stops horizontal movement
    void StopMovement()
    {
        rb.velocity = new Vector3(0, rb.velocity.y, 0);
    }

    // Rotates to face the player
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

    // Performs an attack on the player
    void AttackPlayer()
    {
        RotateToPlayer();                                         // Face player before attacking
        if (animator != null)
        {
            // Reset animation to ensure attack plays
            animator.Play("Idle", 0, 0f);
            Invoke(nameof(ReplayAttackAnimation), 0.05f);        // Slight delay to replay attack
        }

        if (attackPoint == null) return;
        // Check for player in attack range using OverlapSphere
        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Debug.Log($"[{gameObject.name}] Attack hit player! Damage: {attackDamage}", this);
                PlayerController pc = hit.GetComponent<PlayerController>();
                if (pc != null) pc.TakeDamage(attackDamage);
                break;                                             // Only damage player once per attack
            }
        }
    }

    // Replays the attack animation (called via Invoke)
    void ReplayAttackAnimation()
    {
        if (animator != null && currentState == State.Attack)
            animator.Play("Attack", 0, 0f);
    }

    // Called when the enemy takes damage
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
            // Optionally play hit animation
        }
        else
            ChangeState(State.Death);
    }

    // Initializes enemy with game-manager parameters (used when spawning)
    public void InitializeForGame(float health, float speed, Transform playerTarget)
    {
        maxHealth = health;
        currentHealth = health;
        moveSpeed = speed;
        player = playerTarget;

        if (healthBar != null)
            healthBar.InitializeHealth(maxHealth, currentHealth, transform);
    }

    // Destroys the enemy object, notifies GameManager
    private void DestroyEnemy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnEnemyKilled(this);

        if (healthBar != null)
            Destroy(healthBar.gameObject);   // Destroy health bar separately
        Destroy(gameObject);
    }

    // Called by animation event at the end of death animation
    public void OnDeathAnimationComplete()
    {
        CancelInvoke(nameof(DestroyEnemy));  // Cancel any pending Invoke
        DestroyEnemy();                       // Destroy immediately
    }

    // Draw gizmos for chase and attack ranges in the Editor
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