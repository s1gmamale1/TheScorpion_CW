using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// WaveManager — spawns enemy waves, tracks wave progression.
/// Attach to an empty GameObject. Assign spawn points and enemy prefabs in inspector.
/// </summary>
public class WaveManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject basicEnemyPrefab;
    public GameObject fastEnemyPrefab;
    public GameObject heavyEnemyPrefab;
    public GameObject bossPrefab;

    [Header("Spawn Points")]
    public Transform[] spawnPoints; // 4 spawn points on arena walls

    [Header("Timing")]
    public float delayBetweenWaves = 3f;
    public float spawnStagger = 0.3f; // Delay between each enemy in a wave

    // State
    public int CurrentWave { get; private set; } = 0;
    public int TotalWaves => waveData.Length;
    public int EnemiesAlive { get; private set; }
    public bool AllWavesComplete { get; private set; }

    // Events
    public event Action<int, int> OnWaveChanged; // (current, total)
    public event Action<int> OnEnemiesAliveChanged;
    public event Action OnAllWavesComplete;

    private List<GameObject> aliveEnemies = new List<GameObject>();

    // Wave definitions: [Basic, Fast, Heavy, Boss]
    private int[,] waveData = new int[,]
    {
        { 3, 0, 0, 0 },  // Wave 1
        { 5, 0, 0, 0 },  // Wave 2
        { 3, 1, 0, 0 },  // Wave 3
        { 4, 2, 0, 0 },  // Wave 4
        { 3, 1, 1, 0 },  // Wave 5
        { 4, 2, 1, 0 },  // Wave 6
        { 2, 3, 1, 0 },  // Wave 7
        { 4, 2, 2, 0 },  // Wave 8
        { 0, 4, 2, 0 },  // Wave 9
        { 0, 0, 0, 1 },  // Wave 10: Boss
    };

    void Start()
    {
        StartCoroutine(StartNextWave());
    }

    IEnumerator StartNextWave()
    {
        if (CurrentWave >= TotalWaves)
        {
            AllWavesComplete = true;
            OnAllWavesComplete?.Invoke();
            GameManager.Instance?.Victory();
            yield break;
        }

        // Delay between waves (skip for first wave)
        if (CurrentWave > 0)
            yield return new WaitForSeconds(delayBetweenWaves);

        CurrentWave++;
        OnWaveChanged?.Invoke(CurrentWave, TotalWaves);
        Debug.Log($"--- WAVE {CurrentWave}/{TotalWaves} ---");

        int waveIndex = CurrentWave - 1;
        int basicCount = waveData[waveIndex, 0];
        int fastCount = waveData[waveIndex, 1];
        int heavyCount = waveData[waveIndex, 2];
        int bossCount = waveData[waveIndex, 3];

        // Spawn enemies with stagger
        yield return StartCoroutine(SpawnGroup(basicEnemyPrefab, basicCount));
        yield return StartCoroutine(SpawnGroup(fastEnemyPrefab, fastCount));
        yield return StartCoroutine(SpawnGroup(heavyEnemyPrefab, heavyCount));
        yield return StartCoroutine(SpawnGroup(bossPrefab, bossCount));
    }

    IEnumerator SpawnGroup(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) yield break;

        for (int i = 0; i < count; i++)
        {
            // Pick random spawn point
            Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];

            // Add slight random offset so enemies don't stack
            Vector3 offset = new Vector3(
                UnityEngine.Random.Range(-1.5f, 1.5f),
                0f,
                UnityEngine.Random.Range(-1.5f, 1.5f)
            );

            GameObject enemy = Instantiate(prefab, spawnPoint.position + offset, spawnPoint.rotation);
            RegisterEnemy(enemy);

            yield return new WaitForSeconds(spawnStagger);
        }
    }

    void RegisterEnemy(GameObject enemy)
    {
        aliveEnemies.Add(enemy);
        EnemiesAlive++;
        OnEnemiesAliveChanged?.Invoke(EnemiesAlive);

        var health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.OnDeath += () => OnEnemyDied(enemy);
        }
    }

    void OnEnemyDied(GameObject enemy)
    {
        aliveEnemies.Remove(enemy);
        EnemiesAlive--;
        OnEnemiesAliveChanged?.Invoke(EnemiesAlive);

        // Check if wave is cleared
        if (EnemiesAlive <= 0)
        {
            Debug.Log($"Wave {CurrentWave} CLEARED!");
            StartCoroutine(StartNextWave());
        }
    }
}
