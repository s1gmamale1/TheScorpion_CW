using UnityEngine;
using System.Collections;
using Invector.vCharacterController.AI;
using TheScorpion.Core;
using TheScorpion.Data;
using TheScorpion.Systems;

namespace TheScorpion.Enemy
{
    /// <summary>
    /// The Fallen Guardian — 3-phase boss fight.
    /// Phase 1 (100-60%): Sword combos + summon monks every 15s.
    /// Phase 2 (60-30%): Fire aura (damages nearby player) + summon acolytes every 12s.
    /// Phase 3 (30-0%): Enraged — faster, stronger, no summons, relentless.
    /// Extends Invector's AI via events, never modifies source.
    /// </summary>
    public class BossController : MonoBehaviour
    {
        public enum BossPhase { Phase1, Phase2, Phase3, Dead }

        [Header("Phase Thresholds")]
        [SerializeField] private float phase2Threshold = 0.6f;
        [SerializeField] private float phase3Threshold = 0.3f;

        [Header("Summon Settings")]
        [SerializeField] private float phase1SummonInterval = 15f;
        [SerializeField] private float phase2SummonInterval = 12f;
        [SerializeField] private int phase1SummonCount = 2;
        [SerializeField] private int phase2SummonCount = 2;

        [Header("Fire Aura (Phase 2)")]
        [SerializeField] private float auraDamage = 3f;
        [SerializeField] private float auraRadius = 4f;
        [SerializeField] private float auraDamageInterval = 1f;

        [Header("Enrage (Phase 3)")]
        [SerializeField] private float enrageSpeedMultiplier = 1.8f;
        [SerializeField] private float enrageDamageMultiplier = 1.5f;

        // State
        public BossPhase CurrentPhase { get; private set; } = BossPhase.Phase1;
        public string BossName => "The Fallen Guardian";

        private Invector.vHealthController healthController;
        private vSimpleMeleeAI_Controller aiController;
        private UnityEngine.AI.NavMeshAgent agent;
        private Transform playerTransform;

        private float maxHealth;
        private float summonTimer;
        private float auraTimer;
        private float originalSpeed;
        private int originalDamage;
        private GameObject fireAuraVFX;
        private bool isDead;

        // Events for HUD
        public System.Action<BossPhase> OnPhaseChanged;
        public System.Action OnBossDied;

        private void Start()
        {
            healthController = GetComponent<Invector.vHealthController>();
            aiController = GetComponent<vSimpleMeleeAI_Controller>();
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;

            if (healthController != null)
            {
                maxHealth = healthController.MaxHealth;
                healthController.onReceiveDamage.AddListener(OnDamageReceived);
                healthController.onDead.AddListener(OnBossDead);
            }

            if (agent != null) originalSpeed = agent.speed;

            // Configure Phase 1 AI
            ConfigurePhase1();

            Debug.Log($"[Boss] {BossName} has awakened! HP: {maxHealth}");
        }

        private void Update()
        {
            if (isDead || healthController == null) return;

            // Check phase transitions
            float healthPct = healthController.currentHealth / maxHealth;
            BossPhase targetPhase;
            if (healthPct <= 0f) targetPhase = BossPhase.Dead;
            else if (healthPct <= phase3Threshold) targetPhase = BossPhase.Phase3;
            else if (healthPct <= phase2Threshold) targetPhase = BossPhase.Phase2;
            else targetPhase = BossPhase.Phase1;

            if (targetPhase != CurrentPhase)
                TransitionToPhase(targetPhase);

            // Phase-specific behavior
            ExecutePhase();
        }

        private void TransitionToPhase(BossPhase newPhase)
        {
            ExitPhase(CurrentPhase);
            CurrentPhase = newPhase;
            EnterPhase(newPhase);
            OnPhaseChanged?.Invoke(newPhase);
            Debug.Log($"[Boss] Phase transition → {newPhase}");
        }

        private void EnterPhase(BossPhase phase)
        {
            switch (phase)
            {
                case BossPhase.Phase1:
                    ConfigurePhase1();
                    break;

                case BossPhase.Phase2:
                    ConfigurePhase2();
                    // Camera shake on phase transition
                    if (VFX.CameraShakeController.Instance != null)
                        VFX.CameraShakeController.Instance.ShakeHeavy();
                    break;

                case BossPhase.Phase3:
                    ConfigurePhase3();
                    if (VFX.CameraShakeController.Instance != null)
                        VFX.CameraShakeController.Instance.DoShake(5f, 0.5f);
                    break;

                case BossPhase.Dead:
                    isDead = true;
                    break;
            }
        }

        private void ExitPhase(BossPhase phase)
        {
            if (phase == BossPhase.Phase2)
            {
                // Remove fire aura
                if (fireAuraVFX != null) Destroy(fireAuraVFX);
            }
        }

        private void ExecutePhase()
        {
            switch (CurrentPhase)
            {
                case BossPhase.Phase1:
                    HandleSummoning(phase1SummonInterval, phase1SummonCount, false);
                    break;

                case BossPhase.Phase2:
                    HandleSummoning(phase2SummonInterval, phase2SummonCount, true);
                    HandleFireAura();
                    break;

                case BossPhase.Phase3:
                    // No summons — pure aggression
                    break;
            }
        }

        // ==================== PHASE CONFIGS ====================

        private void ConfigurePhase1()
        {
            if (aiController == null) return;
            aiController.chaseSpeed = 0.8f;
            aiController.maxAttackCount = 3;
            aiController.minTimeToAttack = 1.5f;
            aiController.maxTimeToAttack = 3f;
            aiController.strafeDistance = 3f;
            aiController.chanceToBlockInStrafe = 0.2f;
            aiController.chanceToBlockAttack = 0.15f;
            if (agent != null) agent.speed = 3.5f;
        }

        private void ConfigurePhase2()
        {
            if (aiController == null) return;
            aiController.chaseSpeed = 1f;
            aiController.maxAttackCount = 3;
            aiController.minTimeToAttack = 1.2f;
            aiController.maxTimeToAttack = 2.5f;
            aiController.strafeDistance = 2.5f;
            aiController.chanceToBlockInStrafe = 0.15f;
            if (agent != null) agent.speed = 4f;

            // Create fire aura VFX
            fireAuraVFX = CreateFireAura();

            Debug.Log("[Boss] Fire aura activated!");
        }

        private void ConfigurePhase3()
        {
            if (aiController == null) return;
            aiController.chaseSpeed = 1.5f;
            aiController.maxAttackCount = 5;
            aiController.minTimeToAttack = 0.5f;
            aiController.maxTimeToAttack = 1.5f;
            aiController.strafeDistance = 1.5f;
            aiController.chanceToBlockInStrafe = 0f;
            aiController.chanceToBlockAttack = 0f;
            if (agent != null) agent.speed = originalSpeed * enrageSpeedMultiplier;

            Debug.Log("[Boss] ENRAGED! No mercy!");
        }

        // ==================== SUMMONING ====================

        private void HandleSummoning(float interval, int count, bool summonFast)
        {
            summonTimer -= Time.deltaTime;
            if (summonTimer > 0f) return;
            summonTimer = interval;

            if (WaveManager.Instance == null) return;

            for (int i = 0; i < count; i++)
            {
                // Spawn minions near the boss
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist = Random.Range(3f, 6f);
                Vector3 spawnPos = transform.position + new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);

                UnityEngine.AI.NavMeshHit navHit;
                if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out navHit, 10f, UnityEngine.AI.NavMesh.AllAreas))
                    spawnPos = navHit.position;

                // Use WaveManager's prefabs via reflection-free approach: spawn basic or fast
                StartCoroutine(SpawnMinion(spawnPos, summonFast));
            }

            Debug.Log($"[Boss] Summoned {count} {(summonFast ? "acolytes" : "monks")}!");

            // VFX for summon
            SpawnSummonVFX();
        }

        private IEnumerator SpawnMinion(Vector3 position, bool fast)
        {
            // Brief delay for dramatic effect
            yield return new WaitForSeconds(0.3f);

            var wm = WaveManager.Instance;
            if (wm == null) yield break;

            // Access prefabs via the WaveManager's public interface
            // We use SendMessage to trigger a spawn at a specific position
            // Actually, let's just use the same spawn logic
            var prefabField = fast ? "fastEnemyPrefab" : "basicEnemyPrefab";
            var dataField = fast ? "fastEnemyData" : "basicEnemyData";

            // Get prefab/data via reflection (they're serialized private)
            var wmType = wm.GetType();
            var prefab = wmType.GetField(prefabField, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(wm) as GameObject;
            var data = wmType.GetField(dataField, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(wm) as EnemyDataSO;

            if (prefab == null) yield break;

            // Disable agent before instantiate
            var prefabAgent = prefab.GetComponent<UnityEngine.AI.NavMeshAgent>();
            bool agentWas = false;
            if (prefabAgent != null) { agentWas = prefabAgent.enabled; prefabAgent.enabled = false; }

            Quaternion rot = playerTransform != null
                ? Quaternion.LookRotation((playerTransform.position - position).normalized)
                : Quaternion.identity;

            var enemy = Instantiate(prefab, position, rot);
            if (prefabAgent != null) prefabAgent.enabled = agentWas;

            // Enable agent
            var spawnedAgent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (spawnedAgent != null)
            {
                spawnedAgent.enabled = false;
                enemy.transform.position = position;
                spawnedAgent.enabled = true;
            }

            // Init
            var ext = enemy.GetComponent<EnemyExtension>();
            if (ext != null && data != null) ext.Initialize(data, null, null);

            // Behavior
            if (fast && enemy.GetComponent<ShadowAcolyteBehavior>() == null)
                enemy.AddComponent<ShadowAcolyteBehavior>();

            // Health bar
            var hpBar = enemy.AddComponent<UI.EnemyHealthBar>();
            if (data != null) hpBar.SetName(data.enemyName + " (Summoned)");

            // Aggro
            var ai = enemy.GetComponent<vSimpleMeleeAI_Controller>();
            if (ai != null && playerTransform != null) ai.SetCurrentTarget(playerTransform);

            // Hit only player
            var mm = enemy.GetComponent<Invector.vMelee.vMeleeManager>();
            if (mm != null)
            {
                mm.hitProperties.hitDamageTags.Clear();
                mm.hitProperties.hitDamageTags.Add("Player");
            }

            // Auto-destroy after 30s so summoned minions don't pile up
            Destroy(enemy, 30f);
        }

        // ==================== FIRE AURA ====================

        private void HandleFireAura()
        {
            auraTimer -= Time.deltaTime;
            if (auraTimer > 0f) return;
            auraTimer = auraDamageInterval;

            if (playerTransform == null) return;
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist > auraRadius) return;

            // Damage player
            var playerHealth = playerTransform.GetComponent<Invector.vHealthController>();
            if (playerHealth != null && !playerHealth.isDead)
            {
                var dmg = new Invector.vDamage((int)auraDamage);
                dmg.damageType = "Fire";
                dmg.sender = transform;
                dmg.hitReaction = false; // Don't flinch on aura ticks
                playerHealth.TakeDamage(dmg);
            }
        }

        private GameObject CreateFireAura()
        {
            var go = new GameObject("BossFireAura");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.3f, 0.05f);
            light.range = auraRadius * 2f;
            light.intensity = 5f;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.8f;
            main.startSpeed = 2f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.5f, 0f, 0.8f), new Color(1f, 0.2f, 0f, 0.6f));
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.3f;

            var emission = ps.emission;
            emission.rateOverTime = 40;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = auraRadius * 0.5f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.material.color = new Color(1f, 0.4f, 0.1f);

            return go;
        }

        // ==================== SUMMON VFX ====================

        private void SpawnSummonVFX()
        {
            var go = new GameObject("SummonVFX");
            go.transform.position = transform.position + Vector3.up * 0.5f;

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.5f, 0f, 0.8f);
            light.range = 8f;
            light.intensity = 6f;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.6f;
            main.startSpeed = 8f;
            main.startSize = 0.15f;
            main.startColor = new Color(0.6f, 0.1f, 1f, 0.9f);
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 40));

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1f;

            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.material = new Material(Shader.Find("Particles/Standard Unlit"));
            psr.material.color = new Color(0.6f, 0.1f, 1f);

            Destroy(go, 1.5f);
        }

        // ==================== DEATH ====================

        private void OnDamageReceived(Invector.vDamage damage)
        {
            // Phase 3 enrage: boost damage on melee attacks
            if (CurrentPhase == BossPhase.Phase3)
            {
                var mm = GetComponent<Invector.vMelee.vMeleeManager>();
                if (mm != null)
                    mm.defaultDamage.damageValue = 18 * enrageDamageMultiplier;
            }
        }

        private void OnBossDead(GameObject obj)
        {
            isDead = true;
            CurrentPhase = BossPhase.Dead;

            if (fireAuraVFX != null) Destroy(fireAuraVFX);

            OnBossDied?.Invoke();

            Debug.Log($"[Boss] {BossName} has been DEFEATED!");

            // Trigger victory
            if (GameManager.Instance != null)
                GameManager.Instance.SetGameState(GameState.Victory);
        }

        private void OnDestroy()
        {
            if (healthController != null)
            {
                healthController.onReceiveDamage.RemoveListener(OnDamageReceived);
                healthController.onDead.RemoveListener(OnBossDead);
            }
        }
    }
}
