// PlayerController.cs
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Animator _anim;
    private Rigidbody _rb; // New: player Rigidbody component reference

    private BulletLauncher _bulletShoot; // New: bullet launcher component reference
    
    [Header("Movement Settings")]
    [Tooltip("Character movement speed (meters/second)")]
    public float moveSpeed = 5f;
    [Tooltip("Movement smoothness (0-1, smaller = smoother)")]
    public float moveSmoothness = 0.1f; // New: controls Rigidbody movement smoothness

    [Header("View Rotation Settings")]
    [Tooltip("View rotation sensitivity")]
    public float rotateSensitivity = 100f;
    [Tooltip("Enable right-click rotation")]
    public bool enableRightClickRotate = true;

    [Header("Player Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    // For forcing parameter refresh (avoid Animator caching)
    private float lastMoveX;
    private float lastMoveY;
    private bool lastIsMoving;
    
    // Record character's movement direction (based on self orientation)
    private Vector3 _moveDirection;
    private Vector3 _targetVelocity; // New: Rigidbody target velocity

    void Awake()
    {
        // Get animation component
        _anim = GetComponent<Animator>();
        if (_anim == null)
        {
            Debug.LogError("Character missing Animator component!", this);
            enabled = false;
            return;
        }

        // Get Rigidbody component (core modification)
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            Debug.LogError("Character missing Rigidbody component! Please add a Rigidbody before running", this);
            enabled = false;
            return;
        }

        _bulletShoot = GetComponent<BulletLauncher>();
        if (_bulletShoot == null)     
        {
            Debug.LogError("Character missing BulletLauncher component! Please add a bullet launcher before running", this);
            enabled = false;
            return;
        }

        // Initialize Rigidbody parameters (critical: ensure physics collisions work properly)
        _rb.freezeRotation = true; // Freeze rotation (only control Y-axis rotation via code)
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Prevent high-speed tunneling
        _rb.isKinematic = false; // Must be false, otherwise physics collisions won't work

        // Initialize parameters
        lastMoveX = _anim.GetFloat("MoveX");
        lastMoveY = _anim.GetFloat("MoveY");
        lastIsMoving = _anim.GetBool("IsMoving");
        _moveDirection = Vector3.zero;

        currentHealth = maxHealth; // Initialize health
    }

    void Update()
    {
        // 1. Get input in real time (no caching)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 2. Normalize input to prevent diagonal acceleration
        Vector2 input = Vector2.ClampMagnitude(new Vector2(h, v), 1f);

        // 3. Convert to world movement direction based on character's local coordinates
        UpdateMoveDirection(input);

        // 4. Update animation parameters
        UpdateAnimatorParams(input);

        // 5. Left mouse button attack
        if (Input.GetMouseButtonDown(0))
        {
            _anim.SetTrigger("Attack");
            _bulletShoot.LaunchBullet(); // Call bullet launch method
        }

        // 6. Right mouse button hold to rotate view
        if (enableRightClickRotate && Input.GetMouseButton(1))
        {
            RotateCameraAndPlayer();
        }
    }

    void FixedUpdate()
    {
        // Core modification: completely replaced with Rigidbody movement, removed Transform.Translate
        UpdateRigidbodyMovement();
    }

    /// <summary>
    /// Convert input to world movement direction based on character orientation
    /// </summary>
    private void UpdateMoveDirection(Vector2 input)
    {
        // Create local movement direction (forward = Z, right = X)
        Vector3 localMoveDir = new Vector3(input.x, 0, input.y);
        // Convert to world direction (follows character rotation)
        _moveDirection = transform.TransformDirection(localMoveDir);
        // Ensure Y axis is 0 (prevents movement anomalies due to tilting)
        _moveDirection.y = 0;
        _moveDirection = _moveDirection.normalized;

        // Set target velocity for Rigidbody
        _targetVelocity = _moveDirection * moveSpeed;
    }

    /// <summary>
    /// Rigidbody-based physical movement (core replacement)
    /// </summary>
    private void UpdateRigidbodyMovement()
    {
        // Smoothly interpolate to target velocity (avoids movement stutter)
        Vector3 smoothVelocity = Vector3.Lerp(_rb.velocity, _targetVelocity, moveSmoothness / Time.fixedDeltaTime);
        // Modify only X/Z velocity, keep Y axis for physics gravity (e.g., falling, jumping)
        smoothVelocity.y = _rb.velocity.y;
        // Apply velocity to Rigidbody
        _rb.velocity = smoothVelocity;

        // When stationary, reset velocity to prevent sliding
        if (_moveDirection.sqrMagnitude < 0.01f)
        {
            _rb.velocity = new Vector3(0, _rb.velocity.y, 0);
        }
    }

    /// <summary>
    /// Update animation parameters
    /// </summary>
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

    /// <summary>
    /// Rotate camera and player (around Y axis)
    /// </summary>
    private void RotateCameraAndPlayer()
    {
        float mouseX = Input.GetAxis("Mouse X") * rotateSensitivity * Time.deltaTime;
        
        // Rotate player (directly modify Transform, Rigidbody rotation is frozen, no conflict)
        transform.Rotate(Vector3.up, mouseX);
        
        // Rotate camera
        CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.RotateAroundTarget(mouseX);
        }
    }

    // Optional: Reset animation parameters (for scene switching/respawn)
    public void ResetAnimatorParams()
    {
        _anim.SetFloat("MoveX", 0);
        _anim.SetFloat("MoveY", 0);
        _anim.SetBool("IsMoving", false);
        lastMoveX = 0;
        lastMoveY = 0;
        lastIsMoving = false;
        _moveDirection = Vector3.zero;
        _rb.velocity = Vector3.zero; // Reset Rigidbody velocity
    }

    // Optional: Prevent character from being pushed off ground by physics forces
    private void OnCollisionStay(Collision collision)
    {
        // Detect if on ground (adjust Layer as needed)
        if (collision.contacts[0].normal.y > 0.5f)
        {
            _rb.velocity = new Vector3(_rb.velocity.x, 0, _rb.velocity.z);
        }
    }

    /// <summary>
    /// Player damage method (called when attacked by enemies)
    /// </summary>
    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        Debug.Log($"Player remaining health: {currentHealth}");
        
        // Optional: Play hit animation
        // animator.SetTrigger("TakeDamage");
        
        // Death logic when health reaches 0 (add your own as needed)
        if (currentHealth <= 0)
        {
            Debug.Log("Player died!");
            // e.g., stop movement, play death animation, game over, etc.
            enabled = false;
        }
    }
}