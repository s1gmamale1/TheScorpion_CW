using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TheScorpion.Core;
using TheScorpion.Data;
using TheScorpion.Enemy;
using TheScorpion.UI;

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
        [SerializeField] private GameObject elementalNinjaPrefab;
        [SerializeField] private GameObject bossPrefab;

        [Header("Enemy Data SOs")]
        [SerializeField] private EnemyDataSO basicEnemyData;
        [SerializeField] private EnemyDataSO fastEnemyData;
        [SerializeField] private EnemyDataSO heavyEnemyData;
        [SerializeField] private EnemyDataSO elementalNinjaData;
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
        [SerializeField] private bool autoStartWaves = false;

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
        public int TotalKillsAllWaves { get; private set; }
        public bool WavesEnabled { get; private set; } = true;

        public void StartFirstWave()
        {
            WavesEnabled = true;
            StartCoroutine(StartWaveAfterDelay(delayBetweenWaves));
        }

        /// <summary>
        /// Stop spawning new enemies. Current alive enemies remain.
        /// </summary>
        public void DisableWaves()
        {
            WavesEnabled = false;
            if (spawnCoroutine != null)
                StopCoroutine(spawnCoroutine);
            waveInProgress = false;
            Debug.Log("[Wave] Waves DISABLED — no more spawns");
        }

        /// <summary>
        /// Resume wave spawning from where it left off.
        /// </summary>
        public void EnableWaves()
        {
            WavesEnabled = true;
            if (!waveInProgress && enemiesKilledThisWave < totalEnemiesThisWave)
            {
                // Resume current wave
                waveInProgress = true;
                spawnCoroutine = StartCoroutine(ContinuousSpawnLoop());
                Debug.Log($"[Wave] Waves ENABLED — resuming wave {currentWaveIndex}");
            }
            else if (enemiesKilledThisWave >= totalEnemiesThisWave || currentWaveIndex == 0)
            {
                // Start next wave
                StartCoroutine(StartWaveAfterDelay(delayBetweenWaves));
                Debug.Log("[Wave] Waves ENABLED — starting next wave");
            }
        }

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
            acolytesSpawnedThisWave = 0;
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
            // Wave 1: 3, Wave 2: 5, Wave 3: 7, Wave 4: 9, ... (+2 per wave)
            int count = startingEnemies + (wave - 1) * 2;
            return count;
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
        private int acolytesSpawnedThisWave;
        private int elementalSpawnedThisWave;

        private (GameObject prefab, EnemyDataSO data) PickEnemyType()
        {
            if (currentWaveIndex >= totalWaves && bossPrefab != null)
                return (bossPrefab, bossData);

            // Wave 3+: guaranteed tanks — 1 + 1 per 2 waves
            if (currentWaveIndex >= 3)
            {
                int minSentinels = 1 + (currentWaveIndex - 3) / 2;
                if (sentinelsSpawnedThisWave < minSentinels && heavyEnemyPrefab != null)
                {
                    sentinelsSpawnedThisWave++;
                    return (heavyEnemyPrefab, heavyEnemyData);
                }
            }

            // Wave 4+: guaranteed fast acolytes — 1 + 1 per 2 waves
            if (currentWaveIndex >= 4)
            {
                int minAcolytes = 1 + (currentWaveIndex - 4) / 2;
                if (acolytesSpawnedThisWave < minAcolytes && fastEnemyPrefab != null)
                {
                    acolytesSpawnedThisWave++;
                    return (fastEnemyPrefab, fastEnemyData);
                }
            }

            // Wave 6+: guaranteed elemental ninjas — 1 + 1 per 2 waves
            if (currentWaveIndex >= 6)
            {
                int minElemental = 1 + (currentWaveIndex - 6) / 2;
                if (elementalSpawnedThisWave < minElemental)
                {
                    elementalSpawnedThisWave++;
                    // Use elemental prefab if assigned, otherwise fallback to basic
                    var prefab = elementalNinjaPrefab != null ? elementalNinjaPrefab : basicEnemyPrefab;
                    var data = elementalNinjaData != null ? elementalNinjaData : basicEnemyData;
                    return (prefab, data);
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
                    case Core.EnemyType.Elemental:
                        if (enemy.GetComponent<ElementalNinjaBehavior>() == null)
                            enemy.AddComponent<ElementalNinjaBehavior>();
                        break;
                }
            }

            // Make enemies only hit the Player, not each other
            var meleeManager = enemy.GetComponent<Invector.vMelee.vMeleeManager>();
            if (meleeManager != null)
            {
                meleeManager.hitProperties.hitDamageTags.Clear();
                meleeManager.hitProperties.hitDamageTags.Add("Player");
            }

            // Add floating health bar + tint enemy model by type
            var hpBar = enemy.AddComponent<EnemyHealthBar>();
            if (data != null)
            {
                switch (data.enemyType)
                {
                    case Core.EnemyType.Basic:
                        hpBar.SetBarColor(new Color(0.8f, 0.15f, 0.1f)); // red bar
                        hpBar.SetEnemyTint(new Color(0.9f, 0.35f, 0.3f)); // reddish
                        break;
                    case Core.EnemyType.Fast:
                        hpBar.SetBarColor(new Color(0.2f, 0.8f, 0.3f)); // green bar
                        hpBar.SetEnemyTint(new Color(0.3f, 0.85f, 0.35f)); // green
                        break;
                    case Core.EnemyType.Heavy:
                        hpBar.SetBarColor(new Color(0.3f, 0.5f, 0.9f)); // blue bar
                        hpBar.SetEnemyTint(new Color(0.35f, 0.45f, 0.85f)); // blue
                        break;
                    case Core.EnemyType.Elemental:
                        hpBar.SetBarColor(new Color(0.6f, 0.2f, 1f)); // purple bar
                        hpBar.SetEnemyTint(new Color(0.6f, 0.25f, 0.9f)); // purple
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
            TotalKillsAllWaves++;

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
                if (WavesEnabled)
                    StartCoroutine(StartWaveAfterDelay(delayBetweenWaves));
            }
        }

        private void OnAllWavesComplete()
        {
            if (bossPrefab != null)
            {
                Debug.Log("[Scorpion] ALL WAVES COMPLETE — BOSS FIGHT!");
                StartCoroutine(SpawnBossAfterDelay(3f));
            }
            else
            {
                Debug.Log("[Scorpion] ALL WAVES COMPLETE — VICTORY! (no boss prefab)");
                if (onAllWavesClearedEvent != null)
                    onAllWavesClearedEvent.RaiseEvent();
                if (GameManager.Instance != null)
                    GameManager.Instance.SetGameState(GameState.Victory);
            }
        }

        private IEnumerator SpawnBossAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            var player = GetPlayer();
            Vector3 spawnPos = player != null ? player.position + player.forward * 8f : Vector3.zero;

            UnityEngine.AI.NavMeshHit navHit;
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out navHit, 15f, UnityEngine.AI.NavMesh.AllAreas))
                spawnPos = navHit.position;

            Quaternion rot = player != null
                ? Quaternion.LookRotation((player.position - spawnPos).normalized)
                : Quaternion.identity;

            // Disable agent before instantiate
            var prefabAgent = bossPrefab.GetComponent<UnityEngine.AI.NavMeshAgent>();
            bool agentWas = false;
            if (prefabAgent != null) { agentWas = prefabAgent.enabled; prefabAgent.enabled = false; }

            var boss = Instantiate(bossPrefab, spawnPos, rot);

            if (prefabAgent != null) prefabAgent.enabled = agentWas;

            // Enable agent
            var spawnedAgent = boss.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (spawnedAgent != null)
            {
                spawnedAgent.enabled = false;
                boss.transform.position = spawnPos;
                spawnedAgent.enabled = true;
                if (!spawnedAgent.isOnNavMesh && UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out navHit, 20f, UnityEngine.AI.NavMesh.AllAreas))
                    spawnedAgent.Warp(navHit.position);
            }

            // Init data
            var ext = boss.GetComponent<EnemyExtension>();
            if (ext != null && bossData != null)
                ext.Initialize(bossData, onEnemyKilledEvent, onAdrenalineGainEvent);

            // Add boss controller
            if (boss.GetComponent<BossController>() == null)
                boss.AddComponent<BossController>();

            // Health bar + black tint for boss
            var hpBar = boss.AddComponent<EnemyHealthBar>();
            hpBar.SetBarColor(new Color(0.15f, 0.15f, 0.15f)); // black bar
            hpBar.SetEnemyTint(new Color(0.1f, 0.1f, 0.1f)); // black model

            // Aggro
            var ai = boss.GetComponent<Invector.vCharacterController.AI.vSimpleMeleeAI_Controller>();
            if (ai != null && player != null)
                ai.SetCurrentTarget(player);

            // Hit only player
            var mm = boss.GetComponent<Invector.vMelee.vMeleeManager>();
            if (mm != null)
            {
                mm.hitProperties.hitDamageTags.Clear();
                mm.hitProperties.hitDamageTags.Add("Player");
            }

            Debug.Log($"[Wave] BOSS SPAWNED: {bossData?.enemyName ?? "Boss"} | HP: {bossData?.maxHealth ?? 500}");
        }
    }
}
