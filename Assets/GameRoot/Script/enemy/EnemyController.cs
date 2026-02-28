// EnemyController.cs
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Basic Attributes")]
    public float maxHealth = 100f;
    private float currentHealth; // Private, modified only through methods
    public float moveSpeed = 3f;
    public float chaseRange = 8f;   // Chase range
    public float attackRange = 2f;  // Attack range
    public float attackCooldown = 2f;
    public int attackDamage = 20;   // Damage per attack

    [Header("References")]
    public Transform player;
    public Animator animator;
    public Transform attackPoint;   // Attack detection point (optional, for more precise attack range)
    // New: Health bar reference (optional, for synchronizing health display)
    public Enemy3DHealthBar healthBar;
    private Rigidbody rb;           // Rigidbody component reference

    [Header("Debug")]
    public float rotationSpeed = 5f;// Speed of rotating towards player
    public float stopDistance = 0.1f; // Distance threshold to stop moving

    private float lastAttackTime;
    private enum State { Idle, Chase, Attack, Death }
    private State currentState = State.Idle;

    // Expose current health (read-only)
    public float CurrentHealth => currentHealth;
    // Expose max health (read-only)
    public float MaxHealth => maxHealth;

    void Start()
    {
        // Initialize health (single entry point)
        currentHealth = maxHealth;
        
        // Initialize health bar (synchronize initial health)
        if (healthBar != null)
        {
            Debug.Log($"[{gameObject.name}] Initializing health bar, Max health: {maxHealth}, Current health: {currentHealth}", this);
            healthBar.InitializeHealth(maxHealth, currentHealth, transform);
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] healthBar reference not assigned! Health bar cannot be displayed", this);
        }

        // Get and initialize Rigidbody component (must be attached)
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Enemy missing Rigidbody component! Please add and set to non-kinematic", this);
            enabled = false;
            return;
        }
        // Basic Rigidbody settings (essential for physics movement)
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.freezeRotation = true; // Prevent physics rotation causing tilt

        // Initialize animator (null protection)
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("Enemy missing Animator component! Animation control will be disabled", this);
            }
        }

        // Initialize attack point (use self position if not set)
        if (attackPoint == null)
        {
            attackPoint = transform;
            Debug.LogWarning("attackPoint not set, using enemy's own position as attack detection point", this);
        }

        // Initial state: can attack immediately
        lastAttackTime = -attackCooldown;
    }

    void Update()
    {
        if (currentState == State.Death) return;

        // Health check
        if (currentHealth <= 0)
        {
            ChangeState(State.Death);
            return;
        }

        // Safety check: if player is null, idle
        if (player == null)
        {
            ChangeState(State.Idle);
            Debug.LogWarning("Player reference not set! Enemy will remain idle", this);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Simplified state machine (no NavMesh)
        switch (currentState)
        {
            case State.Idle:
                // If player enters chase range, start chasing
                if (distanceToPlayer < chaseRange)
                {
                    ChangeState(State.Chase);
                }
                break;

            case State.Chase:
                // If player leaves chase range, return to idle
                if (distanceToPlayer > chaseRange)
                {
                    ChangeState(State.Idle);
                }
                // If player enters attack range, switch to attack
                else if (distanceToPlayer <= attackRange)
                {
                    ChangeState(State.Attack);
                }
                else
                {
                    // Continue chasing player (pure Rigidbody movement)
                    ChasePlayer();
                }
                break;

            case State.Attack:
                // If player leaves attack range, return to chase
                if (distanceToPlayer > attackRange)
                {
                    ChangeState(State.Chase);
                }
                // Attack when cooldown expires
                else if (Time.time > lastAttackTime + attackCooldown)
                {
                    AttackPlayer();
                    lastAttackTime = Time.time;
                }
                // While on cooldown, keep facing player
                else
                {
                    RotateToPlayer();
                }
                break;
        }
    }

    /// <summary>
    /// Core state change method (adapted to your Animator Bool parameters)
    /// </summary>
    void ChangeState(State newState)
    {
        // Avoid redundant state changes
        if (currentState == newState) return;
        
        currentState = newState;

        // Null protection: skip animator settings if it doesn't exist
        if (animator == null)
        {
            Debug.LogWarning("Animator is null, skipping animation state change", this);
            return;
        }

        switch (newState)
        {
            case State.Idle:
                animator.SetBool("IsIdle", true);
                animator.SetBool("IsChasing", false);
                animator.SetBool("IsAttacking", false);
                StopMovement(); // Stop Rigidbody movement
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
                StopMovement(); // Stop moving while attacking
                break;

            case State.Death:
                animator.SetTrigger("Death");
                StopMovement();
                // Disable collider, Rigidbody, AI
                Collider enemyCollider = GetComponent<Collider>();
                if (enemyCollider != null) enemyCollider.enabled = false;
                
                rb.isKinematic = true;
                enabled = false;
                break;
        }
    }

    /// <summary>
    /// Chase the player (pure Rigidbody physics-driven, no NavMesh)
    /// </summary>
    void ChasePlayer()
    {
        if (player == null) return;

        Vector3 targetDirection = (player.position - transform.position).normalized;
        targetDirection.y = 0; // Ignore height difference

        if (targetDirection.magnitude > stopDistance)
        {
            RotateToPlayer(); // First smoothly turn towards player
            MoveWithRigidbody(targetDirection); // Then move using Rigidbody
        }
        else
        {
            StopMovement();
        }
    }

    /// <summary>
    /// Rigidbody-controlled movement (core method)
    /// </summary>
    /// <param name="direction">Movement direction (normalized)</param>
    void MoveWithRigidbody(Vector3 direction)
    {
        // Calculate target velocity, preserve Y-axis gravity influence
        Vector3 targetVelocity = direction * moveSpeed;
        targetVelocity.y = rb.velocity.y;

        // Smoothly set Rigidbody velocity to avoid teleportation
        rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, Time.deltaTime * 10f);
    }

    /// <summary>
    /// Stop Rigidbody movement
    /// </summary>
    void StopMovement()
    {
        // Stop horizontal movement only, preserve gravity
        rb.velocity = new Vector3(0, rb.velocity.y, 0);
    }

    /// <summary>
    /// Smoothly rotate towards player
    /// </summary>
    void RotateToPlayer()
    {
        if (player == null) return;
        
        Vector3 targetDirection = player.position - transform.position;
        targetDirection.y = 0; // Rotate only around Y axis to prevent tilting

        if (targetDirection.magnitude > stopDistance)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void AttackPlayer()
    {
        // Force face player
        RotateToPlayer();

        // Core fix: force re-trigger Attack animation
        if (animator != null)
        {
            // Briefly switch to Idle, then back to Attack to force reset animation
            animator.Play("Idle", 0, 0f);
            // Switch back to Attack next frame to avoid animation conflict
            Invoke(nameof(ReplayAttackAnimation), 0.05f);
        }

        // Attack range detection (original logic unchanged)
        if (attackPoint == null) return;
        
        Collider[] hitColliders = Physics.OverlapSphere(attackPoint.position, attackRange);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                PlayerController playerController = hitCollider.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.TakeDamage(attackDamage);
                }
                break;
            }
        }
    }

    void ReplayAttackAnimation()
    {
        if (animator != null && currentState == State.Attack)
        {
            animator.Play("Attack", 0, 0f);
        }
    }

    /// <summary>
    /// External call: enemy takes damage (single entry point for health reduction)
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (currentState == State.Death) 
        {
            Debug.LogWarning($"[{gameObject.name}] Already dead, ignoring damage: {damage}", this);
            return; // No damage after death
        }
        
        // Reduce health and clamp to range (0 ~ maxHealth)
        float oldHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        Debug.Log($"[{gameObject.name}] Took damage: {damage}, previous health: {oldHealth}, remaining health: {currentHealth}", this);

        // Synchronize health bar display (replace ?. with explicit check for debugging)
        if (healthBar != null)
        {
            healthBar.UpdateHealthDisplay(currentHealth);
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] healthBar reference is null, cannot update health bar!", this);
        }
        
        // Play hit animation
        if (currentHealth > 0)
        {
            // animator?.SetTrigger("TakeDamage");
        }
        else
        {
            ChangeState(State.Death);
        }
    }

    // Draw auxiliary lines in scene view (keep debugging functionality)
    void OnDrawGizmosSelected()
    {
        // Chase range (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        
        // Attack range (red)
        Gizmos.color = Color.red;
        if (attackPoint != null)
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        else
            Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}