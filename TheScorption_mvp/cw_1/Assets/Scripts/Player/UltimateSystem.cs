using UnityEngine;
using System.Collections;
using TheScorpion.Core;
using TheScorpion.Data;

namespace TheScorpion.Player
{
    public class UltimateSystem : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float maxAdrenaline = 100f;
        [SerializeField] private float ultimateDuration = 8f;
        [SerializeField] private float timeSlowFactor = 0.5f;
        [SerializeField] private float damageMultiplier = 2.5f;
        [SerializeField] private float attackSpeedBonus = 0.3f;

        [Header("Continuous AoE During Ultimate")]
        [SerializeField] private float auraTickDamage = 12f;
        [SerializeField] private float auraTickRadius = 5f;
        [SerializeField] private float auraTickInterval = 0.5f;

        [Header("Adrenaline Gain")]
        [SerializeField] private float adrenalinePerHit = 1.2f;
        [SerializeField] private float adrenalinePerKill = 3f;
        [SerializeField] private float adrenalinePerFinisher = 6f;
        [SerializeField] private float adrenalineOnDamageTaken = 0.5f;

        [Header("References")]
        [SerializeField] private ElementSystem elementSystem;

        [Header("Event Channels")]
        [SerializeField] private VoidEventChannelSO onEnemyKilledEvent;

        [Header("State")]
        [SerializeField] private float currentAdrenaline;
        private bool isUltimateActive;
        private Animator playerAnimator;

        public float CurrentAdrenaline => currentAdrenaline;
        public float MaxAdrenaline => maxAdrenaline;
        public float AdrenalineNormalized => currentAdrenaline / maxAdrenaline;
        public bool IsUltimateActive => isUltimateActive;
        public bool IsUltimateReady => currentAdrenaline >= maxAdrenaline;

        private void Awake()
        {
            playerAnimator = GetComponent<Animator>();
            // Auto-find ElementSystem if not assigned in Inspector
            if (elementSystem == null)
                elementSystem = GetComponent<ElementSystem>();
        }

        private void OnEnable()
        {
            if (onEnemyKilledEvent != null)
                onEnemyKilledEvent.OnEventRaised += OnEnemyKilled;
        }

        private void OnDisable()
        {
            if (onEnemyKilledEvent != null)
                onEnemyKilledEvent.OnEventRaised -= OnEnemyKilled;
        }

        public void AddAdrenaline(float amount)
        {
            if (isUltimateActive) return;
            currentAdrenaline = Mathf.Min(currentAdrenaline + amount, maxAdrenaline);
        }

        public void AddAdrenalineForHit(float styleMultiplier = 1f)
        {
            AddAdrenaline(adrenalinePerHit * styleMultiplier);
        }

        public void AddAdrenalineForFinisher(float styleMultiplier = 1f)
        {
            AddAdrenaline(adrenalinePerFinisher * styleMultiplier);
        }

        public void AddAdrenalineForDamageTaken()
        {
            AddAdrenaline(adrenalineOnDamageTaken);
        }

        private void OnEnemyKilled()
        {
            AddAdrenaline(adrenalinePerKill);
        }

        public float GetDamageMultiplier()
        {
            return isUltimateActive ? damageMultiplier : 1f;
        }

        public void TryActivateUltimate()
        {
            if (!IsUltimateReady || isUltimateActive) return;
            StartCoroutine(UltimateCoroutine());
        }

        private IEnumerator UltimateCoroutine()
        {
            isUltimateActive = true;
            currentAdrenaline = 0f;

            // === ACTIVATION VFX ===
            if (VFX.CameraShakeController.Instance != null)
                VFX.CameraShakeController.Instance.ShakeHeavy();

            SpawnActivationVFX();

            // === SLOW-MO ===
            Time.timeScale = timeSlowFactor;
            Time.fixedDeltaTime = 0.02f * timeSlowFactor;

            if (playerAnimator != null)
            {
                playerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
                playerAnimator.speed = 1f + attackSpeedBonus;
            }

            // Boost player movement to compensate for time slow
            // At timeScale 0.5, multiply speed by 1/0.5 = 2x so player moves at normal speed
            var motor = GetComponent<Invector.vCharacterController.vThirdPersonController>();
            float origWalk = 0f, origRun = 0f, origSprint = 0f;
            if (motor != null)
            {
                origWalk = motor.freeSpeed.walkSpeed;
                origRun = motor.freeSpeed.runningSpeed;
                origSprint = motor.freeSpeed.sprintSpeed;
                float speedCompensation = 1f / timeSlowFactor;
                motor.freeSpeed.walkSpeed *= speedCompensation;
                motor.freeSpeed.runningSpeed *= speedCompensation;
                motor.freeSpeed.sprintSpeed *= speedCompensation;
            }

            var auraGO = SpawnUltimateAura();

            Debug.Log($"[Scorpion] ULTIMATE ACTIVATED! {ultimateDuration}s — x{damageMultiplier} damage, +{attackSpeedBonus * 100}% speed");

            // === CONTINUOUS AoE DAMAGE during ultimate ===
            float elapsed = 0f;
            while (elapsed < ultimateDuration)
            {
                DealAuraTick();
                yield return new WaitForSecondsRealtime(auraTickInterval);
                elapsed += auraTickInterval;
            }

            // === BURST FINALE ===
            PerformElementalBurst();

            if (auraGO != null) Destroy(auraGO);

            // Restore everything
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            if (playerAnimator != null)
            {
                playerAnimator.updateMode = AnimatorUpdateMode.Normal;
                playerAnimator.speed = 1f;
            }

            if (motor != null)
            {
                motor.freeSpeed.walkSpeed = origWalk;
                motor.freeSpeed.runningSpeed = origRun;
                motor.freeSpeed.sprintSpeed = origSprint;
            }

            isUltimateActive = false;
            Debug.Log("[Scorpion] Ultimate ended");
        }

        private void SpawnActivationVFX()
        {
            // Expanding shockwave ring
            var ringGO = new GameObject("Ultimate_ActivationRing");
            ringGO.transform.position = transform.position + Vector3.up * 0.1f;

            // Bright flash light
            var flash = ringGO.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.color = Color.white;
            flash.range = 20f;
            flash.intensity = 8f;

            // Upward burst particles
            var ps = ringGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.6f;
            main.startSpeed = 15f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startColor = new Color(1f, 0.9f, 0.5f, 1f);
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.5f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            var burst = new ParticleSystem.Burst(0f, 60);
            emission.SetBurst(0, burst);

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1f;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

            var renderer = ringGO.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"));
            renderer.material.color = new Color(1f, 0.9f, 0.5f);

            Destroy(ringGO, 1.5f);
        }

        private GameObject SpawnUltimateAura()
        {
            var auraGO = new GameObject("Ultimate_Aura");
            auraGO.transform.SetParent(transform);
            auraGO.transform.localPosition = Vector3.up * 1f;

            // Bright pulsing light
            var light = auraGO.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.85f, 0.3f);
            light.range = 8f;
            light.intensity = 5f;

            // Swirling energy particles
            var ps = auraGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 4f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.9f, 0.4f, 1f),
                new Color(1f, 0.6f, 0.1f, 0.8f));
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.5f;

            var emission = ps.emission;
            emission.rateOverTime = 50;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.8f;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.9f, 0.4f), 0f),
                    new GradientColorKey(new Color(1f, 0.5f, 0.1f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLife.color = grad;

            var renderer = auraGO.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"));
            renderer.material.color = new Color(1f, 0.85f, 0.3f);

            return auraGO;
        }

        private void DealAuraTick()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, auraTickRadius, LayerMask.GetMask("Enemy"));
            foreach (var hit in hits)
            {
                var health = hit.GetComponentInParent<Invector.vHealthController>();
                if (health != null && !health.isDead)
                {
                    var dmg = new Invector.vDamage((int)auraTickDamage);
                    dmg.sender = transform;
                    if (elementSystem != null)
                        dmg.damageType = elementSystem.ActiveElement.ToString();
                    health.TakeDamage(dmg);
                }
            }
        }

        private void PerformElementalBurst()
        {
            // Auto-find if serialized ref was lost
            if (elementSystem == null)
                elementSystem = GetComponent<ElementSystem>();

            float burstRadius = 10f;
            float burstDamage = 80f;
            float burstStun = 2f;
            string damageType = "Fire";

            // Override with ElementDataSO values if available
            if (elementSystem != null)
            {
                damageType = elementSystem.ActiveElement.ToString();
                var data = elementSystem.GetActiveData();
                if (data != null)
                {
                    burstRadius = data.burstRadius > 0 ? data.burstRadius : burstRadius;
                    burstDamage = data.burstDamage > 0 ? data.burstDamage : burstDamage;
                    burstStun = data.burstStunDuration;
                }
            }

            // === BURST VFX ===
            SpawnBurstVFX(burstRadius);

            // Heavy camera shake for burst
            if (VFX.CameraShakeController.Instance != null)
                VFX.CameraShakeController.Instance.DoShake(4f, 0.4f);

            // Damage all enemies in radius
            Collider[] hits = Physics.OverlapSphere(transform.position, burstRadius, LayerMask.GetMask("Enemy"));
            foreach (var hit in hits)
            {
                var health = hit.GetComponentInParent<Invector.vHealthController>();
                if (health != null && !health.isDead)
                {
                    var dmg = new Invector.vDamage((int)burstDamage);
                    dmg.damageType = damageType;
                    dmg.sender = transform;
                    health.TakeDamage(dmg);
                }

                if (burstStun > 0f)
                {
                    var statusEffects = hit.GetComponentInParent<Enemy.EnemyStatusEffects>();
                    if (statusEffects != null)
                        statusEffects.ApplyStun(burstStun);
                }
            }

            Debug.Log($"[Scorpion] ELEMENTAL BURST ({damageType})! Hit {hits.Length} enemies for {burstDamage} damage");
        }

        private void SpawnBurstVFX(float radius)
        {
            Color burstColor = elementSystem.ActiveElement == Core.ElementType.Fire
                ? new Color(1f, 0.4f, 0.1f)
                : new Color(0.3f, 0.7f, 1f);

            var burstGO = new GameObject("Ultimate_Burst");
            burstGO.transform.position = transform.position;

            // Massive flash
            var light = burstGO.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = burstColor;
            light.range = radius * 2f;
            light.intensity = 12f;

            // Explosion particles
            var ps = burstGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1f;
            main.startSpeed = radius * 2f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startColor = new ParticleSystem.MinMaxGradient(burstColor, Color.white);
            main.maxParticles = 100;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 100));

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(burstColor, 0.3f),
                    new GradientColorKey(burstColor * 0.5f, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.3f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLife.color = grad;

            var renderer = burstGO.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"));
            renderer.material.color = burstColor;

            Destroy(burstGO, 2f);
        }
    }
}
