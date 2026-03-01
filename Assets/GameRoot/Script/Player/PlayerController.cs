// PlayerController.cs
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Animator _anim;
    private Rigidbody _rb;
    private BulletLauncher _bulletShoot;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float moveSmoothness = 0.1f;

    [Header("Camera Rotation Settings")]
    public float rotateSensitivity = 100f;
    public bool enableRightClickRotate = true;

    [Header("Player Health")]
    public float maxHealth = 100f;
    public float currentHealth;
    public HealthBar3D healthBar;

    [Header("Attack Attributes")]
    public float baseDamage = 20f;
    public float currentDamage;
    public float baseAttackCooldown = 0.5f;
    public float currentAttackCooldown;
    private float lastAttackTime;

    // Cached animation parameters
    private float lastMoveX;
    private float lastMoveY;
    private bool lastIsMoving;

    private Vector3 _moveDirection;
    private Vector3 _targetVelocity;

    // Properties for UI
    public float CurrentDamage => currentDamage;
    public float CurrentAttackCooldown => currentAttackCooldown;

    void Awake()
    {
        _anim = GetComponent<Animator>();
        if (_anim == null)
        {
            Debug.LogError("Character missing Animator component!", this);
            enabled = false;
            return;
        }

        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            Debug.LogError("Character missing Rigidbody component! Please add a rigidbody before running", this);
            enabled = false;
            return;
        }

        _bulletShoot = GetComponent<BulletLauncher>();
        if (_bulletShoot == null)
        {
            Debug.LogError("Character missing BulletLauncher component! Please add bullet launcher component before running", this);
            enabled = false;
            return;
        }

        _rb.freezeRotation = true;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _rb.isKinematic = false;

        lastMoveX = _anim.GetFloat("MoveX");
        lastMoveY = _anim.GetFloat("MoveY");
        lastIsMoving = _anim.GetBool("IsMoving");

        currentHealth = maxHealth;
        currentDamage = baseDamage;
        currentAttackCooldown = baseAttackCooldown;
        lastAttackTime = -currentAttackCooldown;
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

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector2 input = Vector2.ClampMagnitude(new Vector2(h, v), 1f);

        UpdateMoveDirection(input);
        UpdateAnimatorParams(input);

        if (Input.GetMouseButtonDown(0) && Time.time > lastAttackTime + currentAttackCooldown)
        {
            _anim.SetTrigger("Attack");
            _bulletShoot.LaunchBullet(currentDamage);
            lastAttackTime = Time.time;
        }

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

    private void UpdateMoveDirection(Vector2 input)
    {
        Vector3 localMoveDir = new Vector3(input.x, 0, input.y);
        _moveDirection = transform.TransformDirection(localMoveDir);
        _moveDirection.y = 0;
        _moveDirection = _moveDirection.normalized;

        _targetVelocity = _moveDirection * moveSpeed;
    }

    private void UpdateRigidbodyMovement()
    {
        Vector3 smoothVelocity = Vector3.Lerp(_rb.velocity, _targetVelocity, moveSmoothness / Time.fixedDeltaTime);
        smoothVelocity.y = _rb.velocity.y;
        _rb.velocity = smoothVelocity;

        if (_moveDirection.sqrMagnitude < 0.01f)
        {
            _rb.velocity = new Vector3(0, _rb.velocity.y, 0);
        }
    }

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

    private void RotateCameraAndPlayer()
    {
        float mouseX = Input.GetAxis("Mouse X") * rotateSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up, mouseX);

        CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.RotateAroundTarget(mouseX);
        }
    }

    public void ResetAnimatorParams()
    {
        _anim.SetFloat("MoveX", 0);
        _anim.SetFloat("MoveY", 0);
        _anim.SetBool("IsMoving", false);
        lastMoveX = lastMoveY = 0;
        lastIsMoving = false;
        _moveDirection = Vector3.zero;
        _rb.velocity = Vector3.zero;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.contacts[0].normal.y > 0.5f)
        {
            _rb.velocity = new Vector3(_rb.velocity.x, 0, _rb.velocity.z);
        }
    }

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
            Invoke(nameof(HandleDeath), 2f);
        }
    }

    public void HandleDeath()
    {
        enabled = false;
        if (GameManager.Instance != null)
            GameManager.Instance.PlayerDied();
    }

    // Kill rewards
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        if (healthBar != null) healthBar.UpdateHealthDisplay(currentHealth);
    }

    public void AddDamageBonus(float bonus)
    {
        currentDamage += bonus;
    }

    public void AddAttackSpeedBonus(float bonus)
    {
        currentAttackCooldown = Mathf.Max(0.1f, currentAttackCooldown - bonus);
    }

    // Reset player for new game
    public void ResetPlayer()
    {
        currentHealth = maxHealth;
        currentDamage = baseDamage;
        currentAttackCooldown = baseAttackCooldown;
        lastAttackTime = -currentAttackCooldown;
        if (healthBar != null) healthBar.UpdateHealthDisplay(currentHealth);
        enabled = true;
        ResetAnimatorParams();
    }
}