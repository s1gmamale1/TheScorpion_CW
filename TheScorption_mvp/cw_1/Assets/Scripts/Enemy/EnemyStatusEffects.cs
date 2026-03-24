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

        private IEnumerator BurnCoroutine(float damagePerTick, float duration)
        {
            isBurning = true;
            float elapsed = 0f;

            if (agent != null && enemyData != null && enemyData.burnSlowMultiplier < 1f)
                agent.speed = originalSpeed * enemyData.burnSlowMultiplier;

            while (elapsed < duration && healthController != null && !healthController.isDead)
            {
                float actualDamage = damagePerTick;
                if (enemyData != null)
                    actualDamage *= (1f - enemyData.fireResistance);

                var dmg = new Invector.vDamage((int)actualDamage);
                dmg.damageType = "Fire_DoT";
                healthController.TakeDamage(dmg);

                yield return new WaitForSeconds(1f);
                elapsed += 1f;
            }

            if (agent != null) agent.speed = originalSpeed;
            isBurning = false;
        }

        private IEnumerator StunCoroutine(float duration)
        {
            isStunned = true;

            var animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetBool("IsStunned", true);
            }

            if (agent != null)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            yield return new WaitForSeconds(duration);

            isStunned = false;
            if (animator != null)
                animator.SetBool("IsStunned", false);
            if (agent != null)
                agent.isStopped = false;
        }
    }
}
