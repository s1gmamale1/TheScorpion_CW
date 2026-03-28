using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TheScorpion.Core;
using TheScorpion.Data;
using TheScorpion.Enemy;

namespace TheScorpion.Systems
{
    /// <summary>
    /// 10-wave system. Each wave's total enemy count doubles (3→6→12→24...).
    /// Max 10 enemies on screen — when alive count drops below 10, spawn more.
    /// Enemy type mix gets harder as waves progress.
    /// Wave clears when all enemies for that wave have been killed.
    /// Designed for 20-30 min gameplay.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Header("Enemy Prefabs (Invector AI)")]
        [SerializeField] private GameObject basicEnemyPrefab;
        [SerializeField] private GameObject fastEnemyPrefab;
        [SerializeField] private GameObject heavyEnemyPrefab;
        [SerializeField] private GameObject bossPrefab;

        [Header("Enemy Data SOs")]
        [SerializeField] private EnemyDataSO basicEnemyData;
        [SerializeField] private EnemyDataSO fastEnemyData;
        [SerializeField] private EnemyDataSO heavyEnemyData;
        [SerializeField] private EnemyDataSO bossData;

        [Header("Event Channels")]
        [SerializeField] private VoidEventChannelSO onEnemyKilledEvent;
        [SerializeField] private IntEventChannelSO onAdrenalineGainEvent;
        [SerializeField] private IntEventChannelSO onWaveChangedEvent;
        [SerializeField] private VoidEventChannelSO onAllWavesClearedEvent;

        [Header("Wave Settings")]
        [SerializeField] private int totalWaves = 10;
        [SerializeField] private int startingEnemies = 3;
        [SerializeField] private int maxOnScreen = 10;
        [SerializeField] private float spawnInterval = 1.5f;
        [SerializeField] private float delayBetweenWaves = 3f;
        [SerializeField] private bool autoStartWaves = true;

        // State
        private int currentWaveIndex = 0;
        private int enemiesKilledThisWave;
        private int enemiesSpawnedThisWave;
        private int totalEnemiesThisWave;
        private int aliveCount;
        private bool waveInProgress;
        private Coroutine spawnCoroutine;

        public int CurrentWave => currentWaveIndex;
        public int TotalWaves => totalWaves;
        public int EnemiesAlive => aliveCount;
        public bool IsWaveInProgress => waveInProgress;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (autoStartWaves)
                StartCoroutine(StartWaveAfterDelay(delayBetweenWaves));
        }

        private IEnumerator StartWaveAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartNextWave();
        }

        public void StartNextWave()
        {
            currentWaveIndex++;

            if (currentWaveIndex > totalWaves)
            {
                OnAllWavesComplete();
                return;
            }

            // Wave enemy count: 3, 6, 12, 24, 48... capped for sanity
            totalEnemiesThisWave = GetWaveEnemyCount(currentWaveIndex);
            enemiesKilledThisWave = 0;
            enemiesSpawnedThisWave = 0;
            sentinelsSpawnedThisWave = 0;
            elementalSpawnedThisWave = 0;
            aliveCount = 0;
            waveInProgress = true;

            Debug.Log($"[Scorpion] === WAVE {currentWaveIndex}/{totalWaves} === Total enemies: {totalEnemiesThisWave}");

            if (onWaveChangedEvent != null)
                onWaveChangedEvent.RaiseEvent(currentWaveIndex);

            // Start continuous spawn loop
            if (spawnCoroutine != null)
                StopCoroutine(spawnCoroutine);
            spawnCoroutine = StartCoroutine(ContinuousSpawnLoop());
        }

        private int GetWaveEnemyCount(int wave)
        {
            // Wave 1: 3, Wave 2: 6, Wave 3: 12, Wave 4: 24, Wave 5: 48...
            // Cap at reasonable numbers for 20-30 min gameplay
            int count = startingEnemies * (1 << (wave - 1)); // doubles each wave
            return Mathf.Min(count, 200); // safety cap
        }

        private IEnumerator ContinuousSpawnLoop()
        {
            while (enemiesSpawnedThisWave < totalEnemiesThisWave)
            {
                if (aliveCount < maxOnScreen)
                {
                    SpawnRandomEnemy();
                    yield return new WaitForSeconds(spawnInterval);
                }
                else
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }
            // All spawned — just wait for remaining to die
        }

        private void SpawnRandomEnemy()
        {
            if (enemiesSpawnedThisWave >= totalEnemiesThisWave) return;

            // Pick enemy type based on wave progression
            var (prefab, data) = PickEnemyType();
            if (prefab == null) return;

            SpawnEnemy(prefab, data);
            enemiesSpawnedThisWave++;
        }

        // Track guaranteed spawns per wave
        private int sentinelsSpawnedThisWave;
        private int elementalSpawnedThisWave;

        private (GameObject prefab, EnemyDataSO data) PickEnemyType()
        {
            if (currentWaveIndex >= totalWaves && bossPrefab != null)
                return (bossPrefab, bossData);

            // Wave 3+: guaranteed tanks — 3 base + 1 per wave after 3
            // Wave 3=3, Wave 4=4, Wave 5=5, ... Wave 10=10
            if (currentWaveIndex >= 3)
            {
                int minSentinels = currentWaveIndex;
                if (sentinelsSpawnedThisWave < minSentinels && heavyEnemyPrefab != null)
                {
                    sentinelsSpawnedThisWave++;
                    return (heavyEnemyPrefab, heavyEnemyData);
                }
            }

            // Wave 5+: guaranteed elemental ninjas — 5 base + 1 per wave after 5
            // Wave 5=5, Wave 6=6, Wave 7=7, ... Wave 10=10
            if (currentWaveIndex >= 5)
            {
                int minElemental = currentWaveIndex;
                if (elementalSpawnedThisWave < minElemental && fastEnemyPrefab != null)
                {
                    elementalSpawnedThisWave++;
                    return (fastEnemyPrefab, fastEnemyData);
                }
            }

            // Fill rest with basic gooners
            return (basicEnemyPrefab, basicEnemyData);
        }

        private Transform playerTransform;

        private Transform GetPlayer()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
            return playerTransform;
        }

        private Vector3 GetSpawnPositionNearPlayer()
        {
            var player = GetPlayer();
            Vector3 center = player != null ? player.position : Vector3.zero;

            // Random point 5-10m from player
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(5f, 10f);
            Vector3 offset = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            Vector3 spawnPos = center + offset;

            // Snap to NavMesh
            UnityEngine.AI.NavMeshHit navHit;
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out navHit, 10f, UnityEngine.AI.NavMesh.AllAreas))
                return navHit.position;

            // Fallback: try closer
            if (UnityEngine.AI.NavMesh.SamplePosition(center + offset * 0.5f, out navHit, 15f, UnityEngine.AI.NavMesh.AllAreas))
                return navHit.position;

            return center + Vector3.forward * 5f;
        }

        private void SpawnEnemy(GameObject prefab, EnemyDataSO data)
        {
            if (prefab == null) return;

            Vector3 position = GetSpawnPositionNearPlayer();
            var player = GetPlayer();
            Quaternion rotation = player != null
                ? Quaternion.LookRotation((player.position - position).normalized)
                : Quaternion.identity;

            // Disable NavMeshAgent on prefab before instantiation
            var prefabAgent = prefab.GetComponent<UnityEngine.AI.NavMeshAgent>();
            bool agentWasEnabled = false;
            if (prefabAgent != null)
            {
                agentWasEnabled = prefabAgent.enabled;
                prefabAgent.enabled = false;
            }

            var enemy = Instantiate(prefab, position, rotation);

            // Restore prefab agent
            if (prefabAgent != null) prefabAgent.enabled = agentWasEnabled;

            // Enable spawned agent after positioning
            var spawnedAgent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (spawnedAgent != null)
            {
                spawnedAgent.enabled = false;
                enemy.transform.position = position;
                spawnedAgent.enabled = true;

                if (!spawnedAgent.isOnNavMesh)
                {
                    UnityEngine.AI.NavMeshHit navHit;
                    if (UnityEngine.AI.NavMesh.SamplePosition(enemy.transform.position, out navHit, 20f, UnityEngine.AI.NavMesh.AllAreas))
                        spawnedAgent.Warp(navHit.position);
                }
            }

            // Initialize custom components
            var ext = enemy.GetComponent<EnemyExtension>();
            if (ext != null)
                ext.Initialize(data, onEnemyKilledEvent, onAdrenalineGainEvent);

            var statusFx = enemy.GetComponent<EnemyStatusEffects>();
            if (statusFx != null && data != null)
                statusFx.SetEnemyData(data);

            // Add type-specific behavior based on enemy data
            if (data != null)
            {
                switch (data.enemyType)
                {
                    case Core.EnemyType.Fast:
                        if (enemy.GetComponent<ShadowAcolyteBehavior>() == null)
                            enemy.AddComponent<ShadowAcolyteBehavior>();
                        break;
                    case Core.EnemyType.Heavy:
                        if (enemy.GetComponent<StoneSentinelBehavior>() == null)
                            enemy.AddComponent<StoneSentinelBehavior>();
                        break;
                }
            }

            // Force AI to aggro player immediately
            ForceAggroPlayer(enemy);

            // Hook death for tracking + cleanup
            var health = enemy.GetComponent<Invector.vHealthController>();
            if (health != null)
            {
                var enemyRef = enemy; // capture for closure
                health.onDead.AddListener((deadObj) => OnEnemyDied(enemyRef));
                Debug.Log($"[Wave] Spawned {enemy.name} | HP: {health.currentHealth}/{health.MaxHealth} | onDead hooked");
            }

            aliveCount++;
        }

        private void ForceAggroPlayer(GameObject enemy)
        {
            var player = GetPlayer();
            if (player == null) return;

            // Invector Simple Melee AI uses SetCurrentTarget to force chase
            var aiController = enemy.GetComponent<Invector.vCharacterController.AI.vSimpleMeleeAI_Controller>();
            if (aiController != null)
            {
                aiController.SetCurrentTarget(player);
            }
        }

        private List<GameObject> deadBodies = new List<GameObject>();
        private const int MAX_DEAD_BODIES = 5;
        private const float BODY_DESTROY_DELAY = 5f;

        private void OnEnemyDied(GameObject enemy)
        {
            aliveCount = Mathf.Max(0, aliveCount - 1);
            enemiesKilledThisWave++;

            Debug.Log($"[Wave] KILL! {enemiesKilledThisWave}/{totalEnemiesThisWave} | Alive: {aliveCount}");

            // Dead body management — keep max 5 corpses
            if (enemy != null)
            {
                deadBodies.Add(enemy);

                // Remove oldest if over limit
                while (deadBodies.Count > MAX_DEAD_BODIES)
                {
                    var oldest = deadBodies[0];
                    deadBodies.RemoveAt(0);
                    if (oldest != null) Destroy(oldest);
                }

                // Also auto-destroy this body after delay
                Destroy(enemy, BODY_DESTROY_DELAY);
            }

            if (enemiesKilledThisWave >= totalEnemiesThisWave && waveInProgress)
            {
                waveInProgress = false;
                if (spawnCoroutine != null)
                    StopCoroutine(spawnCoroutine);

                Debug.Log($"[Wave] Wave {currentWaveIndex} CLEARED!");
                StartCoroutine(StartWaveAfterDelay(delayBetweenWaves));
            }
        }

        private void OnAllWavesComplete()
        {
            Debug.Log("[Scorpion] ALL WAVES COMPLETE — VICTORY!");

            if (onAllWavesClearedEvent != null)
                onAllWavesClearedEvent.RaiseEvent();

            if (GameManager.Instance != null)
                GameManager.Instance.SetGameState(GameState.Victory);
        }
    }
}
