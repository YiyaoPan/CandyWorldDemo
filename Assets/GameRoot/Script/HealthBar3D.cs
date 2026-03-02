// ====================================================
// HealthBar3D.cs
// ====================================================

using UnityEngine;

public class HealthBar3D : MonoBehaviour
{
    [Header("Health Bar Settings")]
    public Transform barTransform;                // The transform of the bar that scales (e.g., a child quad)
    public Vector3 offset = new Vector3(0, 2, 0); // Offset from target's position

    private Transform targetTransform;            // The target (enemy/player) this health bar follows
    private float maxHealth;                       // Maximum health for scaling
    private float currentHealth;                    // Current health for scaling
    private float barOriginalScaleX;                // Original X scale of the bar (used to compute new scale)

    void Awake()
    {
        if (barTransform != null)
        {
            barOriginalScaleX = barTransform.localScale.x; // Store original scale
            Debug.Log($"[{gameObject.name}] Health bar initialized, original width: {barOriginalScaleX}", this);
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] barTransform not assigned! Health bar scaling will not work", this);
        }
    }

    void Update()
    {
        if (targetTransform != null)
        {
            // Follow the target with offset
            transform.position = targetTransform.position + offset;

            // Make health bar face the camera (billboard effect) but keep it upright
            Vector3 lookDir = Camera.main.transform.position - transform.position;
            lookDir.y = 0;                                           // Ignore vertical component to keep bar upright
            if (lookDir.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] targetTransform not assigned, health bar position cannot update", this);
        }
    }

    // Initializes the health bar with max and current health and sets the target transform
    public void InitializeHealth(float maxHp, float currentHp, Transform targetTrans)
    {
        maxHealth = maxHp;
        currentHealth = currentHp;
        targetTransform = targetTrans;

        Debug.Log($"[{gameObject.name}] Health bar data initialized - max health: {maxHp}, current health: {currentHp}", this);
        UpdateHealthBar();
    }

    // Updates the health display with a new current health value
    public void UpdateHealthDisplay(float newCurrentHealth)
    {
        currentHealth = newCurrentHealth;
        Debug.Log($"[{gameObject.name}] Updating health display: {newCurrentHealth}/{maxHealth}", this);
        UpdateHealthBar();
    }

    // Recalculates and applies the new scale of the health bar based on current health percentage
    private void UpdateHealthBar()
    {
        if (barTransform == null)
        {
            Debug.LogError($"[{gameObject.name}] barTransform is null, cannot update health bar scale", this);
            return;
        }

        if (maxHealth <= 0)
        {
            Debug.LogError($"[{gameObject.name}] maxHealth is 0, cannot calculate health percentage", this);
            return;
        }

        float healthPercent = Mathf.Clamp01(currentHealth / maxHealth);

        if (barOriginalScaleX == 0)
        {
            barOriginalScaleX = barTransform.localScale.x; // Reacquire if zero
            Debug.LogWarning($"[{gameObject.name}] barOriginalScaleX was 0, reacquired as {barOriginalScaleX}", this);
        }

        Vector3 newScale = new Vector3(
            barOriginalScaleX * healthPercent,
            barTransform.localScale.y,
            barTransform.localScale.z
        );

        barTransform.localScale = newScale;
        Debug.Log($"[{gameObject.name}] Health bar scale updated: percentage={healthPercent:F2}, new scale={newScale}", this);
    }
}