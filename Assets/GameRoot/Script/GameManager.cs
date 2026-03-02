using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game State")]
    public bool isGameOver = false;
    public float gameTimer = 0f;               // Survival time (seconds)

    [Header("Player Reference")]
    public PlayerController player;

    [Header("Enemy Spawn Settings")]
    public GameObject[] enemyPrefabs;           // Multiple enemy prefabs
    public Transform[] spawnPoints;              // Spawn points (empty objects)
    public int maxEnemyCount = 20;                // Maximum enemies on the field
    public float spawnInterval = 3f;              // Base spawn interval
    public float minSpawnDistanceFromPlayer = 10f;// Minimum distance from player for spawn

    [Header("Difficulty Scaling (Over Time)")]
    public float difficultyIncreaseInterval = 30f;// Difficulty increases every 30 seconds
    public float healthMultiplier = 1f;           // Current health multiplier
    public float speedMultiplier = 1f;            // Current speed multiplier
    public float chaseRangeMultiplier = 1f;        // Current chase range multiplier
    [Tooltip("Base health for enemy spawn calculation")]
    public float baseEnemyHealth = 100f;
    [Tooltip("Base movement speed")]
    public float baseEnemySpeed = 3f;
    [Tooltip("Base chase range (can be higher for large maps)")]
    public float baseChaseRange = 15f;             // Base chase range

    [Header("Kill Reward Settings")]
    public int kills = 0;                          // Total kills
    public float healthRegenPerKill = 10f;         // Health regained per kill
    public float attackDamageBonusPerKill = 1f;    // Attack damage increase per kill
    public float attackSpeedBonusPerKill = 0.02f;  // Attack cooldown reduction per kill (attack speed increase)

    [Header("UI References (Can be replaced later)")]
    public TextMeshProUGUI killsText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI attackDamageText;
    public TextMeshProUGUI attackSpeedText;
    public GameObject gameOverPanel;

    [Header("Scene Management")]
    public string mainMenuSceneName = "MainScene";  // Main menu scene name (can be modified in Inspector)

    // List of active enemies
    private List<EnemyController> activeEnemies = new List<EnemyController>();
    private Coroutine spawnCoroutine;
    private Coroutine difficultyCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();

        // Start enemy spawning
        spawnCoroutine = StartCoroutine(SpawnEnemyRoutine());

        // Start difficulty increase
        difficultyCoroutine = StartCoroutine(IncreaseDifficultyRoutine());

        // Initialize UI
        if (gameOverPanel) gameOverPanel.SetActive(false);
        UpdateAllUI();
    }

    void Update()
    {
        if (isGameOver) return;

        gameTimer += Time.deltaTime;
        if (timerText) timerText.text = FormatTime(gameTimer);
    }

    // -------------------- Enemy Spawning --------------------
    IEnumerator SpawnEnemyRoutine()
    {
        while (!isGameOver)
        {
            if (activeEnemies.Count < maxEnemyCount)
            {
                SpawnEnemy();
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefabs.Length == 0 || spawnPoints.Length == 0 || player == null) return;

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        // Avoid spawning too close to player
        if (Vector3.Distance(spawnPoint.position, player.transform.position) < minSpawnDistanceFromPlayer)
            return;

        GameObject enemyObj = Instantiate(enemyPrefabs[Random.Range(0, enemyPrefabs.Length)], spawnPoint.position, Quaternion.identity);
        EnemyController enemy = enemyObj.GetComponent<EnemyController>();
        if (enemy != null)
        {
            float scaledHealth = baseEnemyHealth * healthMultiplier;
            float scaledSpeed = baseEnemySpeed * speedMultiplier;

            enemy.InitializeForGame(scaledHealth, scaledSpeed, player.transform);
            enemy.chaseRange = baseChaseRange * chaseRangeMultiplier;

            activeEnemies.Add(enemy);
        }
    }

    // Called by EnemyController when enemy dies
    public void OnEnemyKilled(EnemyController enemy)
    {
        if (!activeEnemies.Contains(enemy)) return;

        activeEnemies.Remove(enemy);
        kills++;

        // Kill rewards
        if (player != null)
            player.Heal(healthRegenPerKill);

        player.AddDamageBonus(attackDamageBonusPerKill);
        player.AddAttackSpeedBonus(attackSpeedBonusPerKill);

        UpdateAllUI();

        // Can play effects, sounds, etc.
    }

    // -------------------- Difficulty Increase --------------------
    IEnumerator IncreaseDifficultyRoutine()
    {
        while (!isGameOver)
        {
            yield return new WaitForSeconds(difficultyIncreaseInterval);
            healthMultiplier += 0.2f;      // Increase by 20% each time
            speedMultiplier += 0.1f;
            chaseRangeMultiplier += 0.5f;   // Increase chase range by 50% every 30 seconds
        }
    }

    // -------------------- Player Death Handling --------------------
    public void PlayerDied()
    {
        isGameOver = true;
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        if (difficultyCoroutine != null) StopCoroutine(difficultyCoroutine);

        // Disable all enemy AI
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null) enemy.enabled = false;
        }

        // Show game over UI
        if (gameOverPanel) gameOverPanel.SetActive(true);
    }

    // -------------------- UI Update --------------------
    void UpdateAllUI()
    {
        if (killsText) killsText.text = kills.ToString();
        if (attackDamageText) attackDamageText.text = player.CurrentDamage.ToString("F0");
        if (attackSpeedText) attackSpeedText.text = player.CurrentAttackCooldown.ToString("F2");
    }

    string FormatTime(float t)
    {
        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    // Public reset method (new game)
    public void RestartGame()
    {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        if (difficultyCoroutine != null) StopCoroutine(difficultyCoroutine);

        foreach (var enemy in activeEnemies)
            if (enemy != null) Destroy(enemy.gameObject);
        activeEnemies.Clear();

        kills = 0;
        gameTimer = 0f;
        healthMultiplier = 1f;
        speedMultiplier = 1f;
        chaseRangeMultiplier = 1f;
        isGameOver = false;

        if (player != null)
            player.ResetPlayer();

        if (gameOverPanel) gameOverPanel.SetActive(false);

        spawnCoroutine = StartCoroutine(SpawnEnemyRoutine());
        difficultyCoroutine = StartCoroutine(IncreaseDifficultyRoutine());
    }

    /// <summary>
    /// Quit to main menu (clear all data, load specified scene)
    /// </summary>
    public void QuitToMainMenu()
    {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        if (difficultyCoroutine != null) StopCoroutine(difficultyCoroutine);

        foreach (var enemy in activeEnemies)
            if (enemy != null) Destroy(enemy.gameObject);
        activeEnemies.Clear();

        kills = 0;
        gameTimer = 0f;
        healthMultiplier = 1f;
        speedMultiplier = 1f;
        chaseRangeMultiplier = 1f;
        isGameOver = false;

        if (gameOverPanel) gameOverPanel.SetActive(false);

        SceneManager.LoadScene(mainMenuSceneName);
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }
}