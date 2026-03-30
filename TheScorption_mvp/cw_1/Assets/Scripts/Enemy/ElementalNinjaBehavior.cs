using UnityEngine;
using System.Collections;
using Invector.vCharacterController.AI;

namespace TheScorpion.Enemy
{
    /// <summary>
    /// Elemental Ninja — ranged support enemy.
    /// Keeps distance from player, throws projectiles, buffs allies, heals tanks.
    /// Added at spawn time by WaveManager for EnemyType.Elemental enemies.
    /// </summary>
    public class ElementalNinjaBehavior : MonoBehaviour
    {
        [Header("Ranged Attack")]
        [SerializeField] private float attackRange = 10f;
        [SerializeField] private float keepDistance = 7f;
        [SerializeField] private float projectileDamage = 8f;
        [SerializeField] private float projectileSpeed = 15f;
        [SerializeField] private float attackCooldown = 2.5f;

        [Header("Support")]
        [SerializeField] private float buffRadius = 8f;
        [SerializeField] private float buffCooldown = 10f;
        [SerializeField] private float buffSpeedMultiplier = 1.5f;
        [SerializeField] private float buffDuration = 5f;
        [SerializeField] private float healAmount = 20f;
        [SerializeField] private float healCooldown = 8f;
        [SerializeField] private float healThreshold = 0.5f;

        // Invector VFX prefabs — loaded at runtime
        private GameObject projectileHitVFX;
        private GameObject healVFXPrefab;

        private vSimpleMeleeAI_Controller aiController;
        private UnityEngine.AI.NavMeshAgent agent;
        private Invector.vHealthController healthController;
        private Transform playerTransform;

        private float attackTimer;
        private float buffTimer;
        private float healTimer;
        private float originalSpeed;

        private void Start()
        {
            aiController = GetComponent<vSimpleMeleeAI_Controller>();
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            healthController = GetComponent<Invector.vHealthController>();

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;

            if (agent != null) originalSpeed = agent.speed;

            // Load Invector VFX prefabs
            projectileHitVFX = Resources.Load<GameObject>("PlasmaExplosionEffect");
            if (projectileHitVFX == null)
                projectileHitVFX = LoadPrefabByName("PlasmaExplosionEffect");
            healVFXPrefab = LoadPrefabByName("Stun Effect");

            ConfigureAI();
        }

        private void ConfigureAI()
        {
            if (aiController == null) return;

            // Keep distance — don't rush in for melee
            aiController.strafeDistance = keepDistance;
            aiController.chaseSpeed = 0.8f;
            aiController.minTimeToAttack = 5f; // rarely melee
            aiController.maxTimeToAttack = 10f;
            aiController.chanceToBlockInStrafe = 0.1f;
            aiController.chanceToBlockAttack = 0.05f;

            if (agent != null)
                agent.speed = 4f;
        }

        private void Update()
        {
            if (healthController != null && healthController.isDead) return;
            if (playerTransform == null) return;

            attackTimer -= Time.deltaTime;
            buffTimer -= Time.deltaTime;
            healTimer -= Time.deltaTime;

            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            // Keep distance — retreat if player gets too close
            if (distToPlayer < keepDistance * 0.6f && agent != null && agent.isOnNavMesh)
            {
                Vector3 retreatDir = (transform.position - playerTransform.position).normalized;
                Vector3 retreatPos = transform.position + retreatDir * 4f;
                UnityEngine.AI.NavMeshHit navHit;
                if (UnityEngine.AI.NavMesh.SamplePosition(retreatPos, out navHit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                    agent.SetDestination(navHit.position);
            }

            // Ranged attack
            if (attackTimer <= 0f && distToPlayer <= attackRange)
            {
                ShootProjectile();
                attackTimer = attackCooldown;
            }

            // Buff allies
            if (buffTimer <= 0f)
            {
                TryBuffAllies();
                buffTimer = buffCooldown;
            }

            // Heal allies
            if (healTimer <= 0f)
            {
                TryHealAllies();
                healTimer = healCooldown;
            }
        }

        private void ShootProjectile()
        {
            if (playerTransform == null) return;

            // Face player
            Vector3 dir = (playerTransform.position + Vector3.up * 1f - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));

            // Create projectile
            Vector3 spawnPos = transform.position + Vector3.up * 1.5f + transform.forward * 1f;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "NinjaProjectile";
            go.transform.position = spawnPos;
            go.transform.localScale = Vector3.one * 0.25f;
            go.layer = LayerMask.NameToLayer("Ignore Raycast");

            // Visual
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.8f, 0.2f, 1f); // purple
                renderer.material.SetColor("_EmissionColor", new Color(1.5f, 0.4f, 2f));
                renderer.material.EnableKeyword("_EMISSION");
            }

            // Light
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.7f, 0.2f, 1f);
            light.range = 3f;
            light.intensity = 2f;

            // Physics
            var col = go.GetComponent<SphereCollider>();
            if (col != null) col.isTrigger = true;
            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;

            // Projectile behavior
            var proj = go.AddComponent<NinjaProjectile>();
            proj.Initialize(dir, projectileDamage, projectileSpeed, projectileHitVFX);

            Destroy(go, 4f);
        }

        private void TryBuffAllies()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, buffRadius, LayerMask.GetMask("Enemy"));
            int buffed = 0;

            foreach (var hit in hits)
            {
                if (hit.transform.root == transform) continue; // skip self

                var allyHealth = hit.GetComponentInParent<Invector.vHealthController>();
                if (allyHealth == null || allyHealth.isDead) continue;

                var allyAgent = hit.GetComponentInParent<UnityEngine.AI.NavMeshAgent>();
                if (allyAgent != null && allyAgent.isOnNavMesh)
                {
                    StartCoroutine(SpeedBuff(allyAgent));
                    buffed++;
                }

                if (buffed >= 3) break; // max 3 allies buffed at once
            }

            if (buffed > 0)
                SpawnBuffVFX();
        }

        private IEnumerator SpeedBuff(UnityEngine.AI.NavMeshAgent allyAgent)
        {
            float origSpeed = allyAgent.speed;
            allyAgent.speed *= buffSpeedMultiplier;
            yield return new WaitForSeconds(buffDuration);
            if (allyAgent != null && allyAgent.isOnNavMesh)
                allyAgent.speed = origSpeed;
        }

        private void TryHealAllies()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, buffRadius, LayerMask.GetMask("Enemy"));

            foreach (var hit in hits)
            {
                if (hit.transform.root == transform) continue;

                var allyHealth = hit.GetComponentInParent<Invector.vHealthController>();
                if (allyHealth == null || allyHealth.isDead) continue;

                float healthPct = allyHealth.currentHealth / allyHealth.MaxHealth;
                if (healthPct < healThreshold)
                {
                    allyHealth.AddHealth((int)healAmount);
                    SpawnHealVFX(hit.transform.root.position);
                    break; // heal one ally per cooldown
                }
            }
        }

        private void SpawnBuffVFX()
        {
            // Purple burst particles for buff
            var go = new GameObject("BuffVFX");
            go.transform.position = transform.position + Vector3.up * 1.5f;

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.8f, 0.2f, 1f);
            light.range = buffRadius;
            light.intensity = 5f;

            // Particle burst
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.8f;
            main.startSpeed = 4f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startColor = new Color(0.7f, 0.2f, 1f, 0.9f);
            main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 30));
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.material.color = new Color(0.7f, 0.2f, 1f);

            Destroy(go, 1.5f);
        }

        private void SpawnHealVFX(Vector3 position)
        {
            // Use Invector's Stun Effect prefab tinted green, or fallback
            if (healVFXPrefab != null)
            {
                var vfx = Instantiate(healVFXPrefab, position + Vector3.up * 1.5f, Quaternion.identity);
                // Tint particles green
                var systems = vfx.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in systems)
                {
                    var main = ps.main;
                    main.startColor = new Color(0.2f, 1f, 0.4f, 0.9f);
                }
                Destroy(vfx, 2f);
            }
            else
            {
                // Fallback: green particle burst
                var go = new GameObject("HealVFX");
                go.transform.position = position + Vector3.up * 1.5f;

                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(0.2f, 1f, 0.3f);
                light.range = 4f;
                light.intensity = 5f;

                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.startLifetime = 1f;
                main.startSpeed = 2f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
                main.startColor = new Color(0.2f, 1f, 0.4f, 0.9f);
                main.maxParticles = 20;
                main.gravityModifier = -0.5f;
                var emission = ps.emission;
                emission.rateOverTime = 0;
                emission.SetBurst(0, new ParticleSystem.Burst(0f, 20));
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 0.5f;
                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
                renderer.material.color = new Color(0.2f, 1f, 0.4f);

                Destroy(go, 2f);
            }
        }

        private static GameObject LoadPrefabByName(string name)
        {
            #if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets($"t:Prefab {name}");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains(name))
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            #endif
            return null;
        }
    }

    /// <summary>
    /// Simple projectile that damages the player on contact.
    /// </summary>
    public class NinjaProjectile : MonoBehaviour
    {
        private Vector3 direction;
        private float damage;
        private float speed;
        private bool hasHit;
        private GameObject hitVFXPrefab;

        public void Initialize(Vector3 dir, float dmg, float spd, GameObject hitVFX = null)
        {
            direction = dir.normalized;
            damage = dmg;
            speed = spd;
            hitVFXPrefab = hitVFX;
        }

        private void Update()
        {
            if (hasHit) return;
            transform.position += direction * speed * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;

            // Only hit Player
            if (!other.CompareTag("Player") && !other.transform.root.CompareTag("Player"))
                return;

            hasHit = true;

            var health = other.GetComponentInParent<Invector.vHealthController>();
            if (health != null && !health.isDead)
            {
                var dmg = new Invector.vDamage((int)damage);
                dmg.damageType = "Elemental";
                dmg.sender = transform;
                health.TakeDamage(dmg);
            }

            // Spawn hit VFX
            if (hitVFXPrefab != null)
            {
                var vfx = Instantiate(hitVFXPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, 2f);
            }

            Destroy(gameObject);
        }
    }
}
