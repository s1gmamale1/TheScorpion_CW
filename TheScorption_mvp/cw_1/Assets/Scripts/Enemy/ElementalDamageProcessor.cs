using UnityEngine;
using TheScorpion.Core;
using TheScorpion.Data;

namespace TheScorpion.Enemy
{
    /// <summary>
    /// Sits on each enemy. Hooks into vHealthController.onStartReceiveDamage to apply
    /// element resistance modifiers and trigger status effects (burn, stun) via EnemyStatusEffects.
    /// </summary>
    public class ElementalDamageProcessor : MonoBehaviour
    {
        [Header("Burn Settings")]
        [SerializeField] private float burnDamagePerTick = 3f;
        [SerializeField] private float burnDuration = 4f;

        [Header("Stun Settings")]
        [SerializeField] private float stunDuration = 1.5f;

        private Invector.vHealthController healthController;
        private EnemyStatusEffects statusEffects;
        private EnemyExtension extension;

        private void Awake()
        {
            healthController = GetComponent<Invector.vHealthController>();
            statusEffects = GetComponent<EnemyStatusEffects>();
            extension = GetComponent<EnemyExtension>();
        }

        private void Start()
        {
            if (healthController != null)
                healthController.onReceiveDamage.AddListener(OnDamageReceived);
        }

        private void OnDamageReceived(Invector.vDamage damage)
        {
            if (healthController.isDead) return;

            var data = extension?.EnemyData;
            string damageType = damage.damageType ?? "";

            switch (damageType)
            {
                case "Fire":
                    // Apply fire resistance
                    if (data != null && data.fireResistance > 0f)
                        damage.ReduceDamage(data.fireResistance * 100f);

                    // Trigger burn if not already burning
                    if (statusEffects != null && !statusEffects.IsBurning)
                        statusEffects.ApplyBurn(burnDamagePerTick, burnDuration);
                    break;

                case "Lightning":
                    // Apply lightning resistance
                    if (data != null && data.lightningResistance > 0f)
                        damage.ReduceDamage(data.lightningResistance * 100f);

                    // Trigger stun if not already stunned
                    if (statusEffects != null && !statusEffects.IsStunned)
                        statusEffects.ApplyStun(stunDuration);
                    break;
            }

            // Feed poise system
            var poiseSystem = GetComponent<EnemyPoiseSystem>();
            if (poiseSystem != null)
                poiseSystem.TakePoiseDamage(damage);
        }

        private void OnDestroy()
        {
            if (healthController != null)
                healthController.onReceiveDamage.RemoveListener(OnDamageReceived);
        }
    }
}
