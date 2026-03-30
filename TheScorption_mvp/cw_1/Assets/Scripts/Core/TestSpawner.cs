using UnityEngine;
using TheScorpion.Systems;
using TheScorpion.Enemy;
using TheScorpion.Data;

namespace TheScorpion.Core
{
    /// <summary>
    /// F9 — Disable waves, spawn all 4 enemy types around player.
    /// F10 — Disable waves, spawn boss instantly.
    /// Attach to any GameObject (e.g., GameManager).
    /// </summary>
    public class TestSpawner : MonoBehaviour
    {
        [Header("Enemy Prefabs")]
        [SerializeField] private GameObject basicPrefab;
        [SerializeField] private GameObject fastPrefab;
        [SerializeField] private GameObject heavyPrefab;
        [SerializeField] private GameObject elementalPrefab;

        [Header("Enemy Data")]
        [SerializeField] private EnemyDataSO basicData;
        [SerializeField] private EnemyDataSO fastData;
        [SerializeField] private EnemyDataSO heavyData;
        [SerializeField] private EnemyDataSO elementalData;

        [Header("Boss")]
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private EnemyDataSO bossData;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
                SpawnAllTypes();
            if (Input.GetKeyDown(KeyCode.F10))
                SpawnBossInstant();
        }

        public void SpawnAllTypes()
        {
            // Disable waves
            if (WaveManager.Instance != null)
                WaveManager.Instance.DisableWaves();

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) { Debug.LogError("[TestSpawner] No player found"); return; }

            Vector3 c = player.transform.position;

            SpawnEnemy(basicPrefab, basicData, c + new Vector3(5, 0, 0), "Basic Monk");
            SpawnEnemy(fastPrefab, fastData, c + new Vector3(-5, 0, 0), "Shadow Acolyte");
            SpawnEnemy(heavyPrefab, heavyData, c + new Vector3(0, 0, 5), "Stone Sentinel");
            SpawnEnemy(elementalPrefab, elementalData, c + new Vector3(0, 0, -5), "Elemental Ninja");

            Debug.Log("[TestSpawner] All 4 enemy types spawned. Waves disabled.");
        }

        private void SpawnEnemy(GameObject prefab, EnemyDataSO data, Vector3 pos, string label)
        {
            if (prefab == null) { Debug.LogWarning($"[TestSpawner] {label} prefab not assigned"); return; }

            // Snap to NavMesh
            UnityEngine.AI.NavMeshHit navHit;
            if (UnityEngine.AI.NavMesh.SamplePosition(pos, out navHit, 10f, UnityEngine.AI.NavMesh.AllAreas))
                pos = navHit.position;

            var player = GameObject.FindGameObjectWithTag("Player");
            Quaternion rot = player != null
                ? Quaternion.LookRotation((player.transform.position - pos).normalized)
                : Quaternion.identity;

            // Disable agent on prefab
            var prefabAgent = prefab.GetComponent<UnityEngine.AI.NavMeshAgent>();
            bool agentWas = false;
            if (prefabAgent != null) { agentWas = prefabAgent.enabled; prefabAgent.enabled = false; }

            var enemy = Instantiate(prefab, pos, rot);

            if (prefabAgent != null) prefabAgent.enabled = agentWas;

            // Position and enable agent
            var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = false;
                enemy.transform.position = pos;
                agent.enabled = true;
                if (!agent.isOnNavMesh && UnityEngine.AI.NavMesh.SamplePosition(pos, out navHit, 20f, UnityEngine.AI.NavMesh.AllAreas))
                    agent.Warp(navHit.position);
            }

            // Init data
            var ext = enemy.GetComponent<EnemyExtension>();
            if (ext != null && data != null)
                ext.Initialize(data, null, null);

            var statusFx = enemy.GetComponent<EnemyStatusEffects>();
            if (statusFx != null && data != null)
                statusFx.SetEnemyData(data);

            // Add behavior
            if (data != null)
            {
                switch (data.enemyType)
                {
                    case EnemyType.Fast:
                        if (enemy.GetComponent<ShadowAcolyteBehavior>() == null)
                            enemy.AddComponent<ShadowAcolyteBehavior>();
                        break;
                    case EnemyType.Heavy:
                        if (enemy.GetComponent<StoneSentinelBehavior>() == null)
                            enemy.AddComponent<StoneSentinelBehavior>();
                        break;
                    case EnemyType.Elemental:
                        if (enemy.GetComponent<ElementalNinjaBehavior>() == null)
                            enemy.AddComponent<ElementalNinjaBehavior>();
                        break;
                }
            }

            // Health bar + name
            var hpBar = enemy.AddComponent<TheScorpion.UI.EnemyHealthBar>();
            if (data != null)
            {
                switch (data.enemyType)
                {
                    case EnemyType.Basic:
                        hpBar.SetBarColor(new Color(0.8f, 0.15f, 0.1f));
                        hpBar.SetEnemyTint(new Color(0.9f, 0.35f, 0.3f));
                        break;
                    case EnemyType.Fast:
                        hpBar.SetBarColor(new Color(0.2f, 0.8f, 0.3f));
                        hpBar.SetEnemyTint(new Color(0.3f, 0.85f, 0.35f));
                        break;
                    case EnemyType.Heavy:
                        hpBar.SetBarColor(new Color(0.3f, 0.5f, 0.9f));
                        hpBar.SetEnemyTint(new Color(0.35f, 0.45f, 0.85f));
                        break;
                    case EnemyType.Elemental:
                        hpBar.SetBarColor(new Color(0.6f, 0.2f, 1f));
                        hpBar.SetEnemyTint(new Color(0.6f, 0.25f, 0.9f));
                        break;
                }
            }

            // Aggro player
            var ai = enemy.GetComponent<Invector.vCharacterController.AI.vSimpleMeleeAI_Controller>();
            if (ai != null && player != null)
                ai.SetCurrentTarget(player.transform);

            // Only hit player
            var mm = enemy.GetComponent<Invector.vMelee.vMeleeManager>();
            if (mm != null)
            {
                mm.hitProperties.hitDamageTags.Clear();
                mm.hitProperties.hitDamageTags.Add("Player");
            }

            Debug.Log($"[TestSpawner] Spawned {label} | HP: {(data != null ? data.maxHealth : 0)} | Type: {(data != null ? data.enemyType.ToString() : "?")}");
        }

        public void SpawnBossInstant()
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.DisableWaves();

            if (bossPrefab == null) { Debug.LogError("[TestSpawner] Boss prefab not assigned"); return; }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) { Debug.LogError("[TestSpawner] No player found"); return; }

            Vector3 spawnPos = player.transform.position + player.transform.forward * 8f;
            UnityEngine.AI.NavMeshHit navHit;
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out navHit, 15f, UnityEngine.AI.NavMesh.AllAreas))
                spawnPos = navHit.position;

            Quaternion rot = Quaternion.LookRotation((player.transform.position - spawnPos).normalized);

            var prefabAgent = bossPrefab.GetComponent<UnityEngine.AI.NavMeshAgent>();
            bool agentWas = false;
            if (prefabAgent != null) { agentWas = prefabAgent.enabled; prefabAgent.enabled = false; }

            var boss = Instantiate(bossPrefab, spawnPos, rot);
            if (prefabAgent != null) prefabAgent.enabled = agentWas;

            var spawnedAgent = boss.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (spawnedAgent != null)
            {
                spawnedAgent.enabled = false;
                boss.transform.position = spawnPos;
                spawnedAgent.enabled = true;
                if (!spawnedAgent.isOnNavMesh && UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out navHit, 20f, UnityEngine.AI.NavMesh.AllAreas))
                    spawnedAgent.Warp(navHit.position);
            }

            var ext = boss.GetComponent<EnemyExtension>();
            if (ext != null && bossData != null)
                ext.Initialize(bossData, null, null);

            if (boss.GetComponent<BossController>() == null)
                boss.AddComponent<BossController>();

            var hpBar = boss.AddComponent<TheScorpion.UI.EnemyHealthBar>();
            hpBar.SetBarColor(new Color(0.15f, 0.15f, 0.15f));
            hpBar.SetEnemyTint(new Color(0.1f, 0.1f, 0.1f));

            var ai = boss.GetComponent<Invector.vCharacterController.AI.vSimpleMeleeAI_Controller>();
            if (ai != null) ai.SetCurrentTarget(player.transform);

            var mm = boss.GetComponent<Invector.vMelee.vMeleeManager>();
            if (mm != null)
            {
                mm.hitProperties.hitDamageTags.Clear();
                mm.hitProperties.hitDamageTags.Add("Player");
            }

            Debug.Log($"[TestSpawner] BOSS SPAWNED! HP: {(bossData != null ? bossData.maxHealth : 500)}. Waves disabled.");
        }
    }
}
