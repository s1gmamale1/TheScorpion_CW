using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TheScorpion.Core;
using TheScorpion.Data;
using TheScorpion.Enemy;

namespace TheScorpion.Systems
{
    /// <summary>
    /// 10-wave spawn progression. Uses WaveDataSO for composition, SpawnPointManager for positions.
    /// Hooks into Invector's vHealthController.onDead per spawned enemy to track kills.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Header("Wave Data")]
        [SerializeField] private WaveDataSO waveData;

        [Header("Enemy Prefabs (Invector AI)")]
        [SerializeField] private GameObject basicEnemyPrefab;   // Hollow Monk
        [SerializeField] private GameObject fastEnemyPrefab;    // Shadow Acolyte
        [SerializeField] private GameObject heavyEnemyPrefab;   // Stone Sentinel
        [SerializeField] private GameObject bossPrefab;         // The Fallen Guardian

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

        [Header("Settings")]
        [SerializeField] private bool autoStartWaves = true;

        // State
        private int currentWaveIndex = -1;
        private int enemiesAliveThisWave;
        private bool waveInProgress;
        private List<GameObject> aliveEnemies = new List<GameObject>();

        public int CurrentWave => currentWaveIndex + 1;
        public int TotalWaves => waveData != null ? waveData.TotalWaves : 0;
        public int EnemiesAlive => enemiesAliveThisWave;
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
                StartNextWave();
        }

        public void StartNextWave()
        {
            if (waveData == null)
            {
                Debug.LogError("[Scorpion] WaveManager: No WaveDataSO assigned!");
                return;
            }

            currentWaveIndex++;

            if (currentWaveIndex >= waveData.TotalWaves)
            {
                OnAllWavesComplete();
                return;
            }

            var wave = waveData.GetWave(currentWaveIndex);
            if (wave == null) return;

            Debug.Log($"[Scorpion] === WAVE {CurrentWave}/{TotalWaves} ===");

            if (onWaveChangedEvent != null)
                onWaveChangedEvent.RaiseEvent(CurrentWave);

            StartCoroutine(SpawnWaveCoroutine(wave));
        }

        private IEnumerator SpawnWaveCoroutine(WaveDefinition wave)
        {
            waveInProgress = true;

            // Pre-wave delay
            yield return new WaitForSeconds(wave.delayBeforeWave);

            // Spawn basic enemies
            for (int i = 0; i < wave.basicEnemyCount; i++)
            {
                SpawnEnemy(basicEnemyPrefab, basicEnemyData);
                yield return new WaitForSeconds(wave.spawnInterval);
            }

            // Spawn fast enemies
            for (int i = 0; i < wave.fastEnemyCount; i++)
            {
                SpawnEnemy(fastEnemyPrefab, fastEnemyData);
                yield return new WaitForSeconds(wave.spawnInterval);
            }

            // Spawn heavy enemies
            for (int i = 0; i < wave.heavyEnemyCount; i++)
            {
                SpawnEnemy(heavyEnemyPrefab, heavyEnemyData);
                yield return new WaitForSeconds(wave.spawnInterval);
            }

            // Spawn boss
            if (wave.isBossWave && bossPrefab != null)
            {
                SpawnEnemy(bossPrefab, bossData);
            }
        }

        private void SpawnEnemy(GameObject prefab, EnemyDataSO data)
        {
            if (prefab == null || SpawnPointManager.Instance == null) return;

            var (position, rotation) = SpawnPointManager.Instance.GetSpawnPointData();
            var enemy = Instantiate(prefab, position, rotation);

            // Initialize our custom components
            var ext = enemy.GetComponent<EnemyExtension>();
            if (ext != null)
            {
                ext.Initialize(data, onEnemyKilledEvent, onAdrenalineGainEvent);
            }

            var statusFx = enemy.GetComponent<EnemyStatusEffects>();
            if (statusFx != null && data != null)
            {
                statusFx.SetEnemyData(data);
            }

            // Hook into Invector's death for wave tracking
            var health = enemy.GetComponent<Invector.vHealthController>();
            if (health != null)
            {
                health.onDead.AddListener((deadObj) => OnEnemyDied(enemy));
            }

            // Set AI target to player
            SetAITarget(enemy);

            aliveEnemies.Add(enemy);
            enemiesAliveThisWave++;
        }

        private void SetAITarget(GameObject enemy)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            // Invector Simple Melee AI detects by tags — ensure enemy has correct detection setup
            // The prefab should already have tagsToDetect = "Player" configured
            // But let's also try to force set the target if the AI has a SetCurrentTarget method
            var aiController = enemy.GetComponent<Invector.vCharacterController.vThirdPersonController>();
            // Invector AI auto-detects by tag, so this should work if prefab is configured correctly
        }

        private void OnEnemyDied(GameObject enemy)
        {
            aliveEnemies.Remove(enemy);
            enemiesAliveThisWave--;

            if (enemiesAliveThisWave <= 0 && waveInProgress)
            {
                waveInProgress = false;
                Debug.Log($"[Scorpion] Wave {CurrentWave} CLEARED!");

                // Small delay then next wave
                StartCoroutine(DelayedNextWave());
            }
        }

        private IEnumerator DelayedNextWave()
        {
            yield return new WaitForSeconds(2f);
            StartNextWave();
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
