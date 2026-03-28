using UnityEngine;
using System.Collections;
using TheScorpion.Data;

namespace TheScorpion.Enemy
{
    public class EnemyStatusEffects : MonoBehaviour
    {
        [SerializeField] private EnemyDataSO enemyData;

        private Invector.vHealthController healthController;
        private UnityEngine.AI.NavMeshAgent agent;
        private bool isStunned;
        private bool isBurning;
        private float originalSpeed;

        public bool IsStunned => isStunned;
        public bool IsBurning => isBurning;

        private void Awake()
        {
            healthController = GetComponent<Invector.vHealthController>();
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) originalSpeed = agent.speed;
        }

        public void SetEnemyData(EnemyDataSO data)
        {
            enemyData = data;
        }

        public void ApplyBurn(float damagePerTick, float duration)
        {
            if (healthController == null || healthController.isDead) return;
            StartCoroutine(BurnCoroutine(damagePerTick, duration));
        }

        public void ApplyStun(float duration)
        {
            if (healthController == null || healthController.isDead) return;
            float actualDuration = duration;
            if (enemyData != null)
                actualDuration *= enemyData.stunDurationMultiplier;
            StartCoroutine(StunCoroutine(actualDuration));
        }

        private bool IsAgentActive()
        {
            return agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled;
        }

        private IEnumerator BurnCoroutine(float damagePerTick, float duration)
        {
            isBurning = true;
            float elapsed = 0f;

            if (IsAgentActive() && enemyData != null && enemyData.burnSlowMultiplier < 1f)
                agent.speed = originalSpeed * enemyData.burnSlowMultiplier;

            while (elapsed < duration && healthController != null && !healthController.isDead)
            {
                float actualDamage = damagePerTick;
                if (enemyData != null)
                    actualDamage *= (1f - enemyData.fireResistance);

                var dmg = new Invector.vDamage((int)actualDamage);
                dmg.damageType = "Fire_DoT";
                dmg.hitReaction = false; // No flinch on DoT ticks
                healthController.TakeDamage(dmg);

                yield return new WaitForSeconds(1f);
                elapsed += 1f;
            }

            if (IsAgentActive()) agent.speed = originalSpeed;
            isBurning = false;
        }

        private IEnumerator StunCoroutine(float duration)
        {
            isStunned = true;

            // Use Invector's TriggerReaction for stun visual instead of custom "IsStunned" param
            var animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("TriggerReaction");
                animator.SetInteger("ReactionID", 0); // Small hit reaction as stun visual
            }

            if (IsAgentActive())
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            yield return new WaitForSeconds(duration);

            isStunned = false;
            if (IsAgentActive())
                agent.isStopped = false;
        }
    }
}
