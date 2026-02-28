// Enemy3DHealthBar.cs
using UnityEngine;

public class Enemy3DHealthBar : MonoBehaviour
{
    [Header("Health Bar Settings")]
    public Transform barTransform; // Transform of the health bar fill
    public Vector3 offset = new Vector3(0, 2, 0);

    private Transform monsterTransform;
    private float maxHealth;
    private float currentHealth;
    private float barOriginalScaleX;

    void Start()
    {
        // Initialize health bar scale
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
        // Update health bar position and orientation
        if (monsterTransform != null)
        {
            transform.position = monsterTransform.position + offset;
            // Correct orientation calculation: keep health bar always facing the camera without flipping
            Vector3 lookDir = Camera.main.transform.position - transform.position;
            lookDir.y = 0; // Rotate only around Y axis
            if (lookDir.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] monsterTransform not assigned, health bar position cannot update", this);
        }
    }

    /// <summary>
    /// Initialize health bar data (called by EnemyController)
    /// </summary>
    /// <param name="maxHp">Maximum health</param>
    /// <param name="currentHp">Current health</param>
    /// <param name="monsterTrans">Enemy Transform</param>
    public void InitializeHealth(float maxHp, float currentHp, Transform monsterTrans)
    {
        maxHealth = maxHp;
        currentHealth = currentHp;
        monsterTransform = monsterTrans;
        
        Debug.Log($"[{gameObject.name}] Health bar data initialized - Max health: {maxHp}, Current health: {currentHp}", this);
        
        UpdateHealthBar();
    }

    /// <summary>
    /// Update health bar display (called by EnemyController)
    /// </summary>
    /// <param name="newCurrentHealth">New current health value</param>
    public void UpdateHealthDisplay(float newCurrentHealth)
    {
        currentHealth = newCurrentHealth;
        Debug.Log($"[{gameObject.name}] Updating health display: {newCurrentHealth}/{maxHealth}", this);
        UpdateHealthBar();
    }

    // Internal method to update health bar scale
    public void UpdateHealthBar()
    {
        if (barTransform == null)
        {
            Debug.LogError($"[{gameObject.name}] barTransform is null, cannot update health bar scale", this);
            return;
        }
        
        if (maxHealth <= 0)
        {
            Debug.LogError($"[{gameObject.name}] Max health is 0, cannot calculate health percentage", this);
            return;
        }
        
        float healthPercent = currentHealth / maxHealth;
        // Clamp percentage range (prevent negative or exceeding 1)
        healthPercent = Mathf.Clamp01(healthPercent);
        
        Vector3 newScale = new Vector3(
            barOriginalScaleX * healthPercent,
            barTransform.localScale.y,
            barTransform.localScale.z
        );
        
        barTransform.localScale = newScale;
        Debug.Log($"[{gameObject.name}] Health bar scale updated: percentage={healthPercent:F2}, new scale={newScale}", this);
    }
}