// GameManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game State")]
    public bool isGameOver = false;
    public float gameTimer = 0f;

    [Header("Player Reference")]
    public PlayerController player;

    [Header("Enemy Spawn Settings")]
    public GameObject[] enemyPrefabs;
    public Transform[] spawnPoints;
    public int maxEnemyCount = 20;
    public float spawnInterval = 3f;
    public float minSpawnDistanceFromPlayer = 10f;

    [Header("Difficulty Scaling")]
    public float difficultyIncreaseInterval = 30f;
    public float healthMultiplier = 1f;
    public float speedMultiplier = 1f;
    public float chaseRangeMultiplier = 1f;
    [Tooltip("Base health for enemies")]
    public float baseEnemyHealth = 100f;
    [Tooltip("Base speed for enemies")]
    public float baseEnemySpeed = 3f;
    [Tooltip("Base chase range (can be larger for big maps)")]
    public float baseChaseRange = 15f;

    [Header("Kill Rewards")]
    public int kills = 0;
    public float healthRegenPerKill = 10f;
    public float attackDamageBonusPerKill = 1f;
    public float attackSpeedBonusPerKill = 0.02f;

    [Header("UI References")]
    public TextMeshProUGUI killsText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI attackDamageText;
    public TextMeshProUGUI attackSpeedText;
    public GameObject gameOverPanel;

    private List<EnemyController> activeEnemies = new List<EnemyController>();
    private Coroutine spawnCoroutine;
    private Coroutine difficultyCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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

        spawnCoroutine = StartCoroutine(SpawnEnemyRoutine());
        difficultyCoroutine = StartCoroutine(IncreaseDifficultyRoutine());

        if (gameOverPanel) gameOverPanel.SetActive(false);
        UpdateAllUI();
    }

    void Update()
    {
        if (isGameOver) return;

        gameTimer += Time.deltaTime;
        if (timerText) timerText.text = FormatTime(gameTimer);
    }

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

    public void OnEnemyKilled(EnemyController enemy)
    {
        if (!activeEnemies.Contains(enemy)) return;

        activeEnemies.Remove(enemy);
        kills++;

        if (player != null)
            player.Heal(healthRegenPerKill);

        player.AddDamageBonus(attackDamageBonusPerKill);
        player.AddAttackSpeedBonus(attackSpeedBonusPerKill);

        UpdateAllUI();
    }

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

    public void PlayerDied()
    {
        isGameOver = true;
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        if (difficultyCoroutine != null) StopCoroutine(difficultyCoroutine);

        foreach (var enemy in activeEnemies)
        {
            if (enemy != null) enemy.enabled = false;
        }

        if (gameOverPanel) gameOverPanel.SetActive(true);
    }

    void UpdateAllUI()
    {
        if (killsText) killsText.text = kills.ToString();
        if (attackDamageText) attackDamageText.text = player.CurrentDamage.ToString("");
        if (attackSpeedText) attackSpeedText.text = player.CurrentAttackCooldown.ToString("F2");
    }

    string FormatTime(float t)
    {
        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

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

    void OnDestroy()
    {
        StopAllCoroutines();
    }
}