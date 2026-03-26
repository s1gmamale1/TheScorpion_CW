using UnityEngine;
using TheScorpion.Data;

namespace TheScorpion.Enemy
{
    /// <summary>
    /// Hidden poise/stagger gauge per enemy (inspired by Genshin/ZZZ Daze system).
    /// When poise breaks, enemy enters a stagger window where they take bonus damage.
    /// Lightning adds +20 disruption, Fire adds +5. Poise recovers over time.
    /// </summary>
    public class EnemyPoiseSystem : MonoBehaviour
    {
        [Header("Poise Disruption Values")]
        [SerializeField] private float basePoiseDamage = 5f;
        [SerializeField] private float firePoiseDamage = 5f;
        [SerializeField] private float lightningPoiseDamage = 20f;

        [Header("Stagger")]
        [SerializeField] private float staggerBonusDamageMultiplier = 1.5f;

        private EnemyExtension extension;
        private Invector.vHealthController healthController;
        private Animator animator;

        private float currentPoise;
        private float maxPoise;
        private float poiseRecoveryRate;
        private float staggerDuration;
        private bool isStaggered;
        private float staggerTimer;

        public bool IsStaggered => isStaggered;
        public float PoiseNormalized => maxPoise > 0 ? currentPoise / maxPoise : 1f;

        private void Awake()
        {
            extension = GetComponent<EnemyExtension>();
            healthController = GetComponent<Invector.vHealthController>();
            animator = GetComponent<Animator>();
        }

        private void Start()
        {
            var data = extension?.EnemyData;
            if (data != null)
            {
                maxPoise = data.poiseMax;
                poiseRecoveryRate = data.poiseRecoveryRate;
                staggerDuration = data.staggerDuration;
            }
            else
            {
                maxPoise = 20f;
                poiseRecoveryRate = 5f;
                staggerDuration = 1.5f;
            }
            currentPoise = maxPoise;
        }

        private void Update()
        {
            if (healthController != null && healthController.isDead) return;

            if (isStaggered)
            {
                staggerTimer -= Time.deltaTime;
                if (staggerTimer <= 0f)
                    EndStagger();
            }
            else
            {
                // Recover poise when not staggered
                if (currentPoise < maxPoise)
                    currentPoise = Mathf.Min(maxPoise, currentPoise + poiseRecoveryRate * Time.deltaTime);
            }
        }

        public void TakePoiseDamage(Invector.vDamage damage)
        {
            if (isStaggered || (healthController != null && healthController.isDead)) return;

            string damageType = damage.damageType ?? "";
            float poiseDmg = basePoiseDamage;

            switch (damageType)
            {
                case "Fire":
                    poiseDmg = firePoiseDamage;
                    break;
                case "Lightning":
                    poiseDmg = lightningPoiseDamage;
                    break;
            }

            currentPoise -= poiseDmg;

            if (currentPoise <= 0f)
                TriggerStagger();
        }

        private void TriggerStagger()
        {
            isStaggered = true;
            staggerTimer = staggerDuration;
            currentPoise = 0f;

            // Play stagger reaction via Invector's animator
            if (animator != null)
            {
                animator.SetTrigger("TriggerReaction");
                animator.SetInteger("ReactionID", 1); // Big reaction
            }

            Debug.Log($"[Scorpion] {gameObject.name} STAGGERED for {staggerDuration}s!");
        }

        private void EndStagger()
        {
            isStaggered = false;
            currentPoise = maxPoise;
        }
    }
}
