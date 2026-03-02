using UnityEngine;

public class HealthBar3D : MonoBehaviour
{
    [Header("Health Bar Settings")]
    public Transform barTransform;          // Transform of the inner fill bar
    public Vector3 offset = new Vector3(0, 2, 0);

    private Transform targetTransform;      // The unit to follow (enemy or player)
    private float maxHealth;
    private float currentHealth;
    private float barOriginalScaleX;

    void Awake()
    {
        if (barTransform != null)
        {
            barOriginalScaleX = barTransform.localScale.x;
            Debug.Log($"[{gameObject.name}] Health bar initialized, original width: {barOriginalScaleX}", this);
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] barTransform not assigned! Health bar cannot scale", this);
        }
    }

    void Update()
    {
        if (targetTransform != null)
        {
            transform.position = targetTransform.position + offset;

            // Make health bar always face the camera (only rotate around Y axis)
            Vector3 lookDir = Camera.main.transform.position - transform.position;
            lookDir.y = 0;
            if (lookDir.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] targetTransform not assigned, health bar position cannot be updated", this);
        }
    }

    public void InitializeHealth(float maxHp, float currentHp, Transform targetTrans)
    {
        maxHealth = maxHp;
        currentHealth = currentHp;
        targetTransform = targetTrans;

        Debug.Log($"[{gameObject.name}] Health bar data initialized - max health: {maxHp}, current health: {currentHp}", this);
        UpdateHealthBar();
    }

    public void UpdateHealthDisplay(float newCurrentHealth)
    {
        currentHealth = newCurrentHealth;
        Debug.Log($"[{gameObject.name}] Updating health display: {newCurrentHealth}/{maxHealth}", this);
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (barTransform == null)
        {
            Debug.LogError($"[{gameObject.name}] barTransform is null, cannot update health bar scale", this);
            return;
        }

        if (maxHealth <= 0)
        {
            Debug.LogError($"[{gameObject.name}] maxHealth is zero, cannot calculate health percentage", this);
            return;
        }

        float healthPercent = Mathf.Clamp01(currentHealth / maxHealth);

        // If barOriginalScaleX is still 0, re-fetch it
        if (barOriginalScaleX == 0)
        {
            barOriginalScaleX = barTransform.localScale.x;
            Debug.LogWarning($"[{gameObject.name}] barOriginalScaleX was 0, re-fetched as {barOriginalScaleX}", this);
        }

        Vector3 newScale = new Vector3(
            barOriginalScaleX * healthPercent,
            barTransform.localScale.y,
            barTransform.localScale.z
        );

        barTransform.localScale = newScale;
        Debug.Log($"[{gameObject.name}] Health bar scale updated: percent={healthPercent:F2}, new scale={newScale}", this);
    }
}