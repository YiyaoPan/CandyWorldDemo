// ====================================================
// GameManager.cs
// ====================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;               // Singleton instance

    [Header("Game State")]
    public bool isGameOver = false;                   // Whether the game is over
    public float gameTimer = 0f;                       // Timer for game duration

    [Header("Player Reference")]
    public PlayerController player;                     // Reference to the player

    [Header("Enemy Spawn Settings")]
    public GameObject[] enemyPrefabs;                   // Array of enemy prefabs to spawn
    public Transform[] spawnPoints;                      // Array of spawn points
    public int maxEnemyCount = 20;                       // Maximum number of active enemies
    public float spawnInterval = 3f;                     // Time between spawn attempts
    public float minSpawnDistanceFromPlayer = 10f;       // Minimum distance from player to spawn enemy

    [Header("Difficulty Scaling")]
    public float difficultyIncreaseInterval = 30f;       // Time between difficulty increases
    public float healthMultiplier = 1f;                   // Multiplier for enemy health
    public float speedMultiplier = 1f;                     // Multiplier for enemy speed
    public float chaseRangeMultiplier = 1f;                // Multiplier for enemy chase range
    [Tooltip("Base health for enemy spawn calculation")]
    public float baseEnemyHealth = 100f;                    // Base health value
    [Tooltip("Base movement speed")]
    public float baseEnemySpeed = 3f;                        // Base speed value
    [Tooltip("Base chase range (can be larger for big maps)")]
    public float baseChaseRange = 15f;                       // Base chase range

    [Header("Kill Rewards")]
    public int kills = 0;                                    // Total kills
    public float healthRegenPerKill = 10f;                   // Health restored per kill
    public float attackDamageBonusPerKill = 1f;              // Damage increase per kill
    public float attackSpeedBonusPerKill = 0.02f;            // Attack speed increase per kill (cooldown reduction)

    [Header("UI References")]
    public TextMeshProUGUI killsText;                         // Text for kill count
    public TextMeshProUGUI timerText;                         // Text for timer
    public TextMeshProUGUI attackDamageText;                  // Text for current attack damage
    public TextMeshProUGUI attackSpeedText;                   // Text for current attack speed
    public GameObject gameOverPanel;                           // Panel shown on game over

    [Header("Scene Management")]
    public string mainMenuSceneName = "MainScene";            // Name of main menu scene

    private List<EnemyController> activeEnemies = new List<EnemyController>(); // List of active enemies
    private Coroutine spawnCoroutine;                          // Coroutine for spawning
    private Coroutine difficultyCoroutine;                     // Coroutine for difficulty scaling

    void Awake()
    {
        // Singleton pattern
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
            player = FindObjectOfType<PlayerController>();     // Auto-find player if not assigned

        spawnCoroutine = StartCoroutine(SpawnEnemyRoutine());      // Start spawning enemies
        difficultyCoroutine = StartCoroutine(IncreaseDifficultyRoutine()); // Start difficulty scaling

        if (gameOverPanel) gameOverPanel.SetActive(false);
        UpdateAllUI();
    }

    void Update()
    {
        if (isGameOver) return;

        gameTimer += Time.deltaTime;                               // Update game timer
        if (timerText) timerText.text = FormatTime(gameTimer);    // Update timer UI
    }

    // Coroutine that spawns enemies at intervals
    IEnumerator SpawnEnemyRoutine()
    {
        while (!isGameOver)
        {
            if (activeEnemies.Count < maxEnemyCount)               // If below max count
            {
                SpawnEnemy();
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    // Spawns a single enemy
    void SpawnEnemy()
    {
        if (enemyPrefabs.Length == 0 || spawnPoints.Length == 0 || player == null) return;

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)]; // Pick random spawn point
        // Ensure spawn point is not too close to player
        if (Vector3.Distance(spawnPoint.position, player.transform.position) < minSpawnDistanceFromPlayer)
            return;

        // Instantiate random enemy prefab
        GameObject enemyObj = Instantiate(enemyPrefabs[Random.Range(0, enemyPrefabs.Length)], spawnPoint.position, Quaternion.identity);
        EnemyController enemy = enemyObj.GetComponent<EnemyController>();
        if (enemy != null)
        {
            // Apply difficulty multipliers
            float scaledHealth = baseEnemyHealth * healthMultiplier;
            float scaledSpeed = baseEnemySpeed * speedMultiplier;

            enemy.InitializeForGame(scaledHealth, scaledSpeed, player.transform);
            enemy.chaseRange = baseChaseRange * chaseRangeMultiplier;

            activeEnemies.Add(enemy);                               // Add to active list
        }
    }

    // Called when an enemy is killed
    public void OnEnemyKilled(EnemyController enemy)
    {
        if (!activeEnemies.Contains(enemy)) return;

        activeEnemies.Remove(enemy);
        kills++;

        // Apply kill rewards to player
        if (player != null)
            player.Heal(healthRegenPerKill);

        player.AddDamageBonus(attackDamageBonusPerKill);
        player.AddAttackSpeedBonus(attackSpeedBonusPerKill);

        UpdateAllUI();                                              // Update UI after changes
    }

    // Coroutine that gradually increases difficulty over time
    IEnumerator IncreaseDifficultyRoutine()
    {
        while (!isGameOver)
        {
            yield return new WaitForSeconds(difficultyIncreaseInterval);
            healthMultiplier += 0.2f;
            speedMultiplier += 0.1f;
            chaseRangeMultiplier += 0.5f;
        }
    }

    // Called when player dies
    public void PlayerDied()
    {
        isGameOver = true;
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);         // Stop spawning
        if (difficultyCoroutine != null) StopCoroutine(difficultyCoroutine); // Stop difficulty scaling

        // Disable all enemies
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null) enemy.enabled = false;
        }

        if (gameOverPanel) gameOverPanel.SetActive(true);                  // Show game over panel
    }

    // Updates all UI elements
    void UpdateAllUI()
    {
        if (killsText) killsText.text = kills.ToString();
        if (attackDamageText) attackDamageText.text = player.CurrentDamage.ToString("F0");
        if (attackSpeedText) attackSpeedText.text = player.CurrentAttackCooldown.ToString("F2");
    }

    // Formats time as MM:SS
    string FormatTime(float t)
    {
        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    // Restarts the game (resets state and begins again)
    public void RestartGame()
    {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        if (difficultyCoroutine != null) StopCoroutine(difficultyCoroutine);

        // Destroy all active enemies
        foreach (var enemy in activeEnemies)
            if (enemy != null) Destroy(enemy.gameObject);
        activeEnemies.Clear();

        // Reset game variables
        kills = 0;
        gameTimer = 0f;
        healthMultiplier = 1f;
        speedMultiplier = 1f;
        chaseRangeMultiplier = 1f;
        isGameOver = false;

        if (player != null)
            player.ResetPlayer();

        if (gameOverPanel) gameOverPanel.SetActive(false);

        // Restart coroutines
        spawnCoroutine = StartCoroutine(SpawnEnemyRoutine());
        difficultyCoroutine = StartCoroutine(IncreaseDifficultyRoutine());
    }

    // Quits to main menu scene
    public void QuitToMainMenu()
    {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        if (difficultyCoroutine != null) StopCoroutine(difficultyCoroutine);

        // Destroy all active enemies
        foreach (var enemy in activeEnemies)
            if (enemy != null) Destroy(enemy.gameObject);
        activeEnemies.Clear();

        // Reset game variables
        kills = 0;
        gameTimer = 0f;
        healthMultiplier = 1f;
        speedMultiplier = 1f;
        chaseRangeMultiplier = 1f;
        isGameOver = false;

        if (gameOverPanel) gameOverPanel.SetActive(false);

        SceneManager.LoadScene(mainMenuSceneName);                       // Load main menu
    }

    void OnDestroy()
    {
        StopAllCoroutines();                                            // Clean up coroutines when destroyed
    }
}