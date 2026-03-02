// ====================================================
// PlayerController.cs
// ====================================================

using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    private Animator _anim;                     // Reference to the Animator component
    private Rigidbody _rb;                       // Reference to the Rigidbody component
    private BulletLauncher _bulletShoot;          // Reference to the BulletLauncher component

    [Header("Movement Settings")]
    public float moveSpeed = 5f;                  // Base movement speed
    public float moveSmoothness = 0.1f;           // Smoothing factor for velocity changes

    [Header("Rotation Settings")]
    public float rotateSensitivity = 100f;        // Mouse sensitivity for rotation
    public bool enableRightClickRotate = true;    // Whether right-click rotates camera/player

    [Header("Player Health")]
    public float maxHealth = 100f;                 // Maximum health
    public float currentHealth;                    // Current health
    public HealthBar3D healthBar;                   // Reference to the 3D health bar

    [Header("Attack Attributes")]
    public float baseDamage = 20f;                  // Base damage per bullet
    public float currentDamage;                     // Current damage (after bonuses)
    public float baseAttackCooldown = 0.5f;         // Base time between shots
    public float currentAttackCooldown;             // Current cooldown (after bonuses)
    private float lastAttackTime;                    // Time of last attack
    private bool isAttacking = false;                 // Whether attack button is held

    private float lastMoveX;                         // Last MoveX animator parameter value
    private float lastMoveY;                         // Last MoveY animator parameter value
    private bool lastIsMoving;                        // Last IsMoving animator parameter value

    private Vector3 _moveDirection;                   // Desired movement direction in world space
    private Vector3 _targetVelocity;                   // Desired velocity

    public float CurrentDamage => currentDamage;              // Public getter
    public float CurrentAttackCooldown => currentAttackCooldown; // Public getter

    void Awake()
    {
        _anim = GetComponent<Animator>();
        if (_anim == null)
        {
            Debug.LogError("Player missing Animator component!", this);
            enabled = false;
            return;
        }

        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            Debug.LogError("Player missing Rigidbody component! Please add a Rigidbody before running", this);
            enabled = false;
            return;
        }

        _bulletShoot = GetComponent<BulletLauncher>();
        if (_bulletShoot == null)
        {
            Debug.LogError("Player missing BulletLauncher component! Please add bullet launch component before running", this);
            enabled = false;
            return;
        }

        _rb.freezeRotation = true;                             // Prevent physics rotation
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Better collision detection
        _rb.isKinematic = false;                               // Ensure Rigidbody is dynamic

        // Store initial animator parameter values
        lastMoveX = _anim.GetFloat("MoveX");
        lastMoveY = _anim.GetFloat("MoveY");
        lastIsMoving = _anim.GetBool("IsMoving");

        currentHealth = maxHealth;
        currentDamage = baseDamage;
        currentAttackCooldown = baseAttackCooldown;
        lastAttackTime = -currentAttackCooldown;               // Allow immediate first attack
    }

    void Start()
    {
        if (healthBar != null)
        {
            healthBar.InitializeHealth(maxHealth, currentHealth, transform);
        }
        else
        {
            Debug.LogError("Player health bar reference not assigned!", this);
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        // Get input axes
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 input = Vector2.ClampMagnitude(new Vector2(h, v), 1f); // Normalize diagonal movement

        UpdateMoveDirection(input);
        UpdateAnimatorParams(input);

        // Check for attack input (left mouse button) and ensure not over UI
        bool attackPressed = Input.GetMouseButton(0) && !IsPointerOverUI();
        if (attackPressed != isAttacking)
        {
            isAttacking = attackPressed;
            _anim.SetBool("IsAttacking", isAttacking);
        }

        // If attacking and cooldown passed, launch a bullet
        if (isAttacking && Time.time > lastAttackTime + currentAttackCooldown)
        {
            _bulletShoot.LaunchBullet(currentDamage);
            lastAttackTime = Time.time;
        }

        // Right-click rotation
        if (enableRightClickRotate && Input.GetMouseButton(1))
        {
            RotateCameraAndPlayer();
        }
    }

    void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;
        UpdateRigidbodyMovement();
    }

    // Check if the mouse is over a UI element
    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    // Converts input to world-space movement direction based on player's current rotation
    private void UpdateMoveDirection(Vector2 input)
    {
        Vector3 localMoveDir = new Vector3(input.x, 0, input.y);
        _moveDirection = transform.TransformDirection(localMoveDir); // Transform local to world
        _moveDirection.y = 0;
        _moveDirection = _moveDirection.normalized;

        _targetVelocity = _moveDirection * moveSpeed;
    }

    // Smoothly updates Rigidbody velocity towards target velocity
    private void UpdateRigidbodyMovement()
    {
        Vector3 smoothVelocity = Vector3.Lerp(_rb.velocity, _targetVelocity, moveSmoothness / Time.fixedDeltaTime);
        smoothVelocity.y = _rb.velocity.y;                           // Preserve vertical velocity (gravity)
        _rb.velocity = smoothVelocity;

        if (_moveDirection.sqrMagnitude < 0.01f)                     // If no movement input, stop horizontal movement
        {
            _rb.velocity = new Vector3(0, _rb.velocity.y, 0);
        }
    }

    // Updates animator parameters with input values, only when changed
    private void UpdateAnimatorParams(Vector2 input)
    {
        if (Mathf.Abs(input.x - lastMoveX) > 0.01f)
        {
            _anim.SetFloat("MoveX", input.x);
            lastMoveX = input.x;
        }
        if (Mathf.Abs(input.y - lastMoveY) > 0.01f)
        {
            _anim.SetFloat("MoveY", input.y);
            lastMoveY = input.y;
        }
        bool isMoving = input.sqrMagnitude > 0.01f;
        if (isMoving != lastIsMoving)
        {
            _anim.SetBool("IsMoving", isMoving);
            lastIsMoving = isMoving;
        }
    }

    // Rotates the player and camera horizontally based on mouse movement
    private void RotateCameraAndPlayer()
    {
        float mouseX = Input.GetAxis("Mouse X") * rotateSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up, mouseX);                         // Rotate player

        CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.RotateAroundTarget(mouseX);                 // Rotate camera around player
        }
    }

    // Resets animator parameters and movement (used when restarting)
    public void ResetAnimatorParams()
    {
        _anim.SetFloat("MoveX", 0);
        _anim.SetFloat("MoveY", 0);
        _anim.SetBool("IsMoving", false);
        _anim.SetBool("IsAttacking", false);
        lastMoveX = lastMoveY = 0;
        lastIsMoving = false;
        isAttacking = false;
        _moveDirection = Vector3.zero;
        _rb.velocity = Vector3.zero;
    }

    // Prevents sticking to slopes by zeroing vertical velocity if ground contact is high
    private void OnCollisionStay(Collision collision)
    {
        if (collision.contacts[0].normal.y > 0.5f)   // If collision normal is mostly upward (ground)
        {
            _rb.velocity = new Vector3(_rb.velocity.x, 0, _rb.velocity.z);
        }
    }

    // Called when player takes damage
    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        Debug.Log($"Player remaining health: {currentHealth}");

        if (healthBar != null)
            healthBar.UpdateHealthDisplay(currentHealth);
        else
            Debug.LogError("Player health bar reference is null, cannot update!", this);

        if (currentHealth <= 0)
        {
            Debug.Log("Player died!");
            _anim.SetTrigger("Die");
            isAttacking = false;
            _anim.SetBool("IsAttacking", false);
            Invoke(nameof(HandleDeath), 2f);       // Delay before handling death
        }
    }

    // Handles player death (disables script and notifies GameManager)
    public void HandleDeath()
    {
        enabled = false;
        if (GameManager.Instance != null)
            GameManager.Instance.PlayerDied();
    }

    // Heals the player by a certain amount
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        if (healthBar != null) healthBar.UpdateHealthDisplay(currentHealth);
    }

    // Adds a bonus to damage
    public void AddDamageBonus(float bonus)
    {
        currentDamage += bonus;
    }

    // Adds a bonus to attack speed (reduces cooldown)
    public void AddAttackSpeedBonus(float bonus)
    {
        currentAttackCooldown = Mathf.Max(0.1f, currentAttackCooldown - bonus);
    }

    // Resets player to initial state (used when restarting)
    public void ResetPlayer()
    {
        currentHealth = maxHealth;
        currentDamage = baseDamage;
        currentAttackCooldown = baseAttackCooldown;
        lastAttackTime = -currentAttackCooldown;
        isAttacking = false;
        if (healthBar != null) healthBar.UpdateHealthDisplay(currentHealth);
        enabled = true;
        ResetAnimatorParams();
    }
}