using UnityEngine;
using System.Collections;
using TheScorpion.Core;
using TheScorpion.Combat;
using TheScorpion.Data;

namespace TheScorpion.Player
{
    public class ElementSystem : MonoBehaviour
    {
        [Header("Element Data")]
        [SerializeField] private ElementDataSO fireData;
        [SerializeField] private ElementDataSO lightningData;

        [Header("Energy")]
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private float energyRegenRate = 3f;
        [SerializeField] private float energyPerHit = 5f;

        [Header("Event Channels")]
        [SerializeField] private IntEventChannelSO onElementChangedEvent;

        [Header("Projectile")]
        [SerializeField] private float projectileCooldown = 0.5f;
        [SerializeField] private float projectileEnergyCost = 10f;
        [SerializeField] private float projectileDamage = 12f;
        [SerializeField] private float projectileSpeed = 25f;
        [SerializeField] private GameObject fireProjectilePrefab;
        [SerializeField] private GameObject lightningProjectilePrefab;
        [SerializeField] private GameObject fireProjectileVFX;
        [SerializeField] private GameObject lightningProjectileVFX;

        [Header("State")]
        [SerializeField] private ElementType activeElement = ElementType.Fire;

        private float currentEnergy;
        private float ability1CooldownTimer;
        private float ability2CooldownTimer;
        private float projectileCooldownTimer;
        private bool ability2Active;

        public ElementType ActiveElement => activeElement;
        public float CurrentEnergy => currentEnergy;
        public float MaxEnergy => maxEnergy;
        public float Ability1CooldownNormalized => GetActiveData() != null ? Mathf.Clamp01(ability1CooldownTimer / GetActiveData().ability1Cooldown) : 0f;
        public float Ability2CooldownNormalized => GetActiveData() != null ? Mathf.Clamp01(ability2CooldownTimer / GetActiveData().ability2Cooldown) : 0f;
        public bool IsAbility2Active => ability2Active;

        private void Start()
        {
            currentEnergy = maxEnergy;
        }

        private void Update()
        {
            currentEnergy = Mathf.Min(currentEnergy + energyRegenRate * Time.deltaTime, maxEnergy);

            if (ability1CooldownTimer > 0f)
                ability1CooldownTimer -= Time.deltaTime;
            if (ability2CooldownTimer > 0f)
                ability2CooldownTimer -= Time.deltaTime;
            if (projectileCooldownTimer > 0f)
                projectileCooldownTimer -= Time.deltaTime;
        }

        public void FireProjectile()
        {
            if (projectileCooldownTimer > 0f)
            {
                Debug.Log($"[Scorpion] Projectile on cooldown: {projectileCooldownTimer:F1}s");
                return;
            }
            if (currentEnergy < projectileEnergyCost)
            {
                Debug.Log($"[Scorpion] Not enough energy for projectile ({currentEnergy:F0}/{projectileEnergyCost})");
                return;
            }

            currentEnergy -= projectileEnergyCost;
            projectileCooldownTimer = projectileCooldown;

            // Spawn projectile in front of player
            Vector3 spawnPos = transform.position + transform.forward * 1.5f + Vector3.up * 1.2f;
            Vector3 direction = transform.forward;

            // Auto-aim: find nearest enemy within a cone
            direction = GetAutoAimDirection(spawnPos, direction);

            GameObject prefab = activeElement == ElementType.Fire ? fireProjectilePrefab : lightningProjectilePrefab;

            if (prefab == null)
            {
                // Create a sphere projectile with VFX attached
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = activeElement == ElementType.Fire ? "Fireball" : "LightningBolt";
                go.transform.position = spawnPos;
                go.transform.localScale = Vector3.one * 0.3f;

                // Hide the sphere mesh — VFX will be the visual
                var meshRenderer = go.GetComponent<MeshRenderer>();

                // Pick VFX based on element
                GameObject vfxPrefab = activeElement == ElementType.Fire ? fireProjectileVFX : lightningProjectileVFX;

                if (vfxPrefab != null)
                {
                    // Attach VFX as child of the projectile
                    var vfx = Instantiate(vfxPrefab, go.transform);
                    vfx.transform.localPosition = Vector3.zero;
                    vfx.transform.localScale = Vector3.one * 0.5f;
                    // Hide the sphere since VFX is the visual
                    if (meshRenderer != null) meshRenderer.enabled = false;
                }
                else
                {
                    // Fallback: color the sphere if no VFX
                    if (meshRenderer != null)
                    {
                        meshRenderer.material.color = activeElement == ElementType.Fire
                            ? new Color(1f, 0.4f, 0f)
                            : new Color(0.3f, 0.7f, 1f);
                        meshRenderer.material.SetColor("_EmissionColor",
                            (activeElement == ElementType.Fire ? new Color(2f, 0.8f, 0f) : new Color(0.6f, 1.4f, 2f)));
                        meshRenderer.material.EnableKeyword("_EMISSION");
                    }
                }

                // Add a point light for glow effect
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 3f;
                light.intensity = 2f;
                light.color = activeElement == ElementType.Fire
                    ? new Color(1f, 0.5f, 0.1f)
                    : new Color(0.3f, 0.6f, 1f);

                // Shrink collider to match visual size and make trigger
                var sphereCol = go.GetComponent<SphereCollider>();
                if (sphereCol != null)
                {
                    sphereCol.isTrigger = true;
                    sphereCol.radius = 0.5f;
                }

                // Set to IgnoreRaycast layer so it doesn't hit ground/walls
                go.layer = LayerMask.NameToLayer("Ignore Raycast");

                // Rigidbody for trigger detection
                var rb = go.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = true;

                var projectile = go.AddComponent<ElementalProjectile>();
                projectile.Initialize(direction, projectileDamage, activeElement, projectileSpeed, 3f);

                Debug.Log($"[Scorpion] {activeElement} projectile fired!");
            }
            else
            {
                var go = Instantiate(prefab, spawnPos, Quaternion.LookRotation(direction));
                var projectile = go.GetComponent<ElementalProjectile>();
                if (projectile == null) projectile = go.AddComponent<ElementalProjectile>();
                projectile.Initialize(direction, projectileDamage, activeElement, projectileSpeed, 3f);

                Debug.Log($"[Scorpion] {activeElement} projectile fired!");
            }
        }

        private Vector3 GetAutoAimDirection(Vector3 origin, Vector3 defaultDir)
        {
            float autoAimRange = 20f;
            float autoAimAngle = 45f; // cone half-angle in degrees

            Collider[] enemies = Physics.OverlapSphere(origin, autoAimRange, LayerMask.GetMask("Enemy"));
            if (enemies.Length == 0) return defaultDir;

            Transform bestTarget = null;
            float bestScore = float.MaxValue;

            foreach (var enemy in enemies)
            {
                var health = enemy.GetComponentInParent<Invector.vHealthController>();
                if (health == null || health.isDead) continue;

                Transform enemyRoot = enemy.transform.root;
                Vector3 toEnemy = (enemyRoot.position + Vector3.up * 1f - origin);
                float distance = toEnemy.magnitude;
                float angle = Vector3.Angle(defaultDir, toEnemy.normalized);

                // Only consider enemies within the aim cone
                if (angle > autoAimAngle) continue;

                // Score: prefer closer enemies and those more centered in the cone
                float score = distance + (angle * 0.5f);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = enemyRoot;
                }
            }

            if (bestTarget != null)
            {
                Vector3 aimDir = (bestTarget.position + Vector3.up * 1f - origin).normalized;
                return aimDir;
            }

            return defaultDir;
        }

        public ElementDataSO GetActiveData()
        {
            return activeElement == ElementType.Fire ? fireData : lightningData;
        }

        public void SwitchToNextElement()
        {
            activeElement = activeElement == ElementType.Fire ? ElementType.Lightning : ElementType.Fire;
            Debug.Log($"[Scorpion] Element switched to: {activeElement} | Energy: {currentEnergy:F0}/{maxEnergy}");
            onElementChangedEvent?.RaiseEvent((int)activeElement);
        }

        public void SwitchToPreviousElement()
        {
            SwitchToNextElement();
        }

        public void GainEnergy(float amount)
        {
            currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
        }

        public void UseAbility1()
        {
            var data = GetActiveData();
            if (data == null)
            {
                Debug.LogWarning("[Scorpion] Ability 1: No ElementData assigned! Assign Fire_Data and Lightning_Data in the Inspector.");
                return;
            }
            if (ability1CooldownTimer > 0f)
            {
                Debug.Log($"[Scorpion] Ability 1 on cooldown: {ability1CooldownTimer:F1}s remaining");
                return;
            }
            if (currentEnergy < data.ability1Cost)
            {
                Debug.Log($"[Scorpion] Ability 1: Not enough energy ({currentEnergy:F0}/{data.ability1Cost})");
                return;
            }

            Debug.Log($"[Scorpion] Ability 1 activated: {data.ability1Name} ({activeElement})");
            currentEnergy -= data.ability1Cost;
            ability1CooldownTimer = data.ability1Cooldown;

            if (activeElement == ElementType.Fire)
                StartCoroutine(FireTornado(data));
            else
                LightningBurst(data);
        }

        public void UseAbility2()
        {
            var data = GetActiveData();
            if (data == null)
            {
                Debug.LogWarning("[Scorpion] Ability 2: No ElementData assigned!");
                return;
            }
            if (ability2CooldownTimer > 0f || currentEnergy < data.ability2Cost || ability2Active) return;

            Debug.Log($"[Scorpion] Ability 2 activated: {data.ability2Name} ({activeElement})");
            currentEnergy -= data.ability2Cost;
            ability2CooldownTimer = data.ability2Cooldown;

            if (activeElement == ElementType.Fire)
                StartCoroutine(FireAura(data));
            else
                StartCoroutine(LightningSpeed(data));
        }

        private GameObject SpawnVFXOnPlayer(GameObject prefab, float duration = 3f)
        {
            if (prefab == null) return null;
            var vfx = Instantiate(prefab, transform);
            vfx.transform.localPosition = Vector3.zero;
            Destroy(vfx, duration);
            return vfx;
        }

        private IEnumerator FireTornado(ElementDataSO data)
        {
            SpawnVFXOnPlayer(data.ability1VFXPrefab, data.ability1Duration);

            float elapsed = 0f;
            while (elapsed < data.ability1Duration)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, data.ability1Radius, LayerMask.GetMask("Enemy"));
                foreach (var hit in hits)
                {
                    var health = hit.GetComponentInParent<Invector.vHealthController>();
                    if (health != null && !health.isDead)
                    {
                        var dmg = new Invector.vDamage((int)data.ability1Damage);
                        dmg.damageType = "Fire";
                        dmg.sender = transform;
                        health.TakeDamage(dmg);
                    }
                }
                yield return new WaitForSeconds(1f);
                elapsed += 1f;
            }
        }

        private void LightningBurst(ElementDataSO data)
        {
            SpawnVFXOnPlayer(data.ability1VFXPrefab, 2f);

            Collider[] hits = Physics.OverlapSphere(transform.position, data.ability1Radius, LayerMask.GetMask("Enemy"));
            foreach (var hit in hits)
            {
                var health = hit.GetComponentInParent<Invector.vHealthController>();
                if (health != null && !health.isDead)
                {
                    var dmg = new Invector.vDamage((int)data.ability1Damage);
                    dmg.damageType = "Lightning";
                    dmg.sender = transform;
                    health.TakeDamage(dmg);
                }

                var statusEffects = hit.GetComponentInParent<TheScorpion.Enemy.EnemyStatusEffects>();
                if (statusEffects != null)
                    statusEffects.ApplyStun(1.5f);
            }
        }

        private IEnumerator FireAura(ElementDataSO data)
        {
            ability2Active = true;
            var auraVFX = SpawnVFXOnPlayer(data.ability2VFXPrefab, data.ability2Duration);
            float elapsed = 0f;
            while (elapsed < data.ability2Duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            ability2Active = false;
        }

        private IEnumerator LightningSpeed(ElementDataSO data)
        {
            ability2Active = true;
            var speedVFX = SpawnVFXOnPlayer(data.ability2VFXPrefab, data.ability2Duration);

            var motor = GetComponent<Invector.vCharacterController.vThirdPersonController>();
            if (motor != null)
            {
                var originalSpeed = motor.freeSpeed.walkSpeed;
                motor.freeSpeed.walkSpeed *= (1f + data.ability2MoveSpeedBonus);
            }

            var animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.speed = 1f + data.ability2AttackSpeedBonus;
            }

            yield return new WaitForSeconds(data.ability2Duration);

            if (motor != null)
            {
                motor.freeSpeed.walkSpeed = motor.freeSpeed.walkSpeed / (1f + data.ability2MoveSpeedBonus);
            }
            if (animator != null)
            {
                animator.speed = 1f;
            }

            ability2Active = false;
        }

        public float GetAttackBonusDamage()
        {
            if (!ability2Active || activeElement != ElementType.Fire) return 0f;
            var data = GetActiveData();
            return data != null ? data.ability2BurnDamagePerTick : 0f;
        }
    }

    public class ElementalProjectile : MonoBehaviour
    {
        public float speed = 20f;
        public float damage = 15f;
        public ElementType elementType = ElementType.Fire;

        private Vector3 direction;
        private bool hasHit;

        public void Initialize(Vector3 dir, float dmg, ElementType element, float spd, float lifetime, float radius = 0f)
        {
            direction = dir.normalized;
            damage = dmg;
            elementType = element;
            speed = spd;
            hasHit = false;
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (hasHit) return;
            transform.position += direction * speed * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;

            // Only hit things on the Enemy layer
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (other.gameObject.layer != enemyLayer)
            {
                // Also check parent (enemy hitboxes might be on BodyPart layer)
                var parentObj = other.transform.root.gameObject;
                if (parentObj.layer != enemyLayer) return;
            }

            hasHit = true;

            var health = other.GetComponentInParent<Invector.vHealthController>();
            if (health != null && !health.isDead)
            {
                var dmg = new Invector.vDamage((int)damage);
                dmg.damageType = elementType.ToString();
                dmg.sender = transform;
                health.TakeDamage(dmg);
            }

            var statusEffects = other.GetComponentInParent<TheScorpion.Enemy.EnemyStatusEffects>();
            if (statusEffects != null)
            {
                if (elementType == ElementType.Fire)
                    statusEffects.ApplyBurn(5f, 3f);
                else if (elementType == ElementType.Lightning)
                    statusEffects.ApplyStun(1.5f);
            }

            Debug.Log($"[Scorpion] {elementType} projectile hit: {other.gameObject.name}");

            // Detach VFX children so they linger as hit effects
            foreach (Transform child in transform)
            {
                child.SetParent(null);
                // Stop any particle systems from emitting new particles
                var ps = child.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var emission = ps.emission;
                    emission.enabled = false;
                }
                // Also check child particle systems
                foreach (var childPS in child.GetComponentsInChildren<ParticleSystem>())
                {
                    var em = childPS.emission;
                    em.enabled = false;
                }
                Destroy(child.gameObject, 1.5f);
            }

            Destroy(gameObject);
        }
    }
}
