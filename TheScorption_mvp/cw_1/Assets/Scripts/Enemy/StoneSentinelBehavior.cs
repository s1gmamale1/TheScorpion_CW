using UnityEngine;
using Invector.vCharacterController.AI;

namespace TheScorpion.Enemy
{
    /// <summary>
    /// Stone Sentinel (Tank) — slow, tanky, blocks often, heavy hits.
    /// Walks in, takes hits, blocks, occasional heavy slam.
    /// 50% light attack damage reduction handled by EnemyDataSO.lightAttackReduction.
    /// </summary>
    public class StoneSentinelBehavior : MonoBehaviour
    {
        [Header("Chase")]
        [SerializeField] private float chaseSpeed = 0.5f;

        [Header("Attack")]
        [SerializeField] private int attackComboCount = 1;
        [SerializeField] private float minTimeBetweenAttacks = 3f;
        [SerializeField] private float maxTimeBetweenAttacks = 5f;
        [SerializeField] private float attackDistance = 2f;

        [Header("Defense")]
        [SerializeField] private float blockChanceStrafe = 0.4f;
        [SerializeField] private float blockChanceAttack = 0.3f;

        private vSimpleMeleeAI_Controller aiController;
        private UnityEngine.AI.NavMeshAgent agent;
        private Invector.vHealthController healthController;

        private void Start()
        {
            aiController = GetComponent<vSimpleMeleeAI_Controller>();
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            healthController = GetComponent<Invector.vHealthController>();

            ConfigureInvectorAI();

            // Hook damage received for light attack reduction
            if (healthController != null)
                healthController.onReceiveDamage.AddListener(OnDamageReceived);
        }

        private void ConfigureInvectorAI()
        {
            if (aiController == null) return;

            // Slow and steady
            aiController.chaseSpeed = chaseSpeed;

            // Heavy single hits, long wind-up
            aiController.maxAttackCount = attackComboCount;
            aiController.minTimeToAttack = minTimeBetweenAttacks;
            aiController.maxTimeToAttack = maxTimeBetweenAttacks;

            // Stay at medium range, strafe slowly
            aiController.strafeDistance = 3f;

            // Blocks a lot
            aiController.chanceToBlockInStrafe = blockChanceStrafe;
            aiController.chanceToBlockAttack = blockChanceAttack;

            // Slow NavMesh
            if (agent != null)
                agent.speed = 2f;
        }

        private void OnDamageReceived(Invector.vDamage damage)
        {
            // Light attack reduction — check if it's a basic melee hit (no element = light attack)
            // Heavy attacks would have higher base damage, abilities have damageType set
            var ext = GetComponent<EnemyExtension>();
            if (ext != null && ext.EnemyData != null)
            {
                float reduction = ext.EnemyData.lightAttackReduction;
                if (reduction > 0f && string.IsNullOrEmpty(damage.damageType))
                {
                    // Reduce damage from non-elemental (light) attacks
                    damage.ReduceDamage(reduction * 100f);
                }
            }
        }

        private void OnDestroy()
        {
            if (healthController != null)
                healthController.onReceiveDamage.RemoveListener(OnDamageReceived);
        }
    }
}
