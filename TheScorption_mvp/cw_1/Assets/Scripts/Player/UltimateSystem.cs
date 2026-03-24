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
        [SerializeField] private float damageMultiplier = 1.5f;

        [Header("Adrenaline Gain")]
        [SerializeField] private float adrenalinePerHit = 2f;
        [SerializeField] private float adrenalinePerKill = 5f;
        [SerializeField] private float adrenalinePerFinisher = 10f;
        [SerializeField] private float adrenalineOnDamageTaken = 1f;

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

            Time.timeScale = timeSlowFactor;
            Time.fixedDeltaTime = 0.02f * timeSlowFactor;

            if (playerAnimator != null)
                playerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;

            yield return new WaitForSecondsRealtime(ultimateDuration);

            PerformElementalBurst();

            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            if (playerAnimator != null)
                playerAnimator.updateMode = AnimatorUpdateMode.Normal;

            isUltimateActive = false;
        }

        private void PerformElementalBurst()
        {
            if (elementSystem == null) return;
            var data = elementSystem.GetActiveData();
            if (data == null) return;

            Collider[] hits = Physics.OverlapSphere(transform.position, data.burstRadius, LayerMask.GetMask("Enemy"));
            foreach (var hit in hits)
            {
                var health = hit.GetComponentInParent<Invector.vHealthController>();
                if (health != null && !health.isDead)
                {
                    var dmg = new Invector.vDamage((int)data.burstDamage);
                    dmg.damageType = elementSystem.ActiveElement.ToString();
                    dmg.sender = transform;
                    health.TakeDamage(dmg);
                }

                if (data.burstStunDuration > 0f)
                {
                    var statusEffects = hit.GetComponentInParent<Enemy.EnemyStatusEffects>();
                    if (statusEffects != null)
                        statusEffects.ApplyStun(data.burstStunDuration);
                }
            }
        }
    }
}
