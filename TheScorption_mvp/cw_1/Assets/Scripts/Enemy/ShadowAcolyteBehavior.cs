using UnityEngine;
using System.Collections;
using Invector.vCharacterController.AI;

namespace TheScorpion.Enemy
{
    /// <summary>
    /// Shadow Acolyte (Fast Ninja) — hit-and-run pattern.
    /// Sprint in → quick 2-hit combo → retreat → circle → repeat.
    /// Hooks into Invector's vSimpleMeleeAI_Controller and overrides settings.
    /// </summary>
    public class ShadowAcolyteBehavior : MonoBehaviour
    {
        [Header("Chase")]
        [SerializeField] private float chaseSpeed = 1.5f;
        [SerializeField] private float sprintToPlayerDistance = 8f;

        [Header("Attack")]
        [SerializeField] private int attackComboCount = 2;
        [SerializeField] private float minTimeBetweenAttacks = 1f;
        [SerializeField] private float maxTimeBetweenAttacks = 2f;
        [SerializeField] private float attackDistance = 1.5f;

        [Header("Retreat")]
        [SerializeField] private float retreatDistance = 6f;
        [SerializeField] private float retreatDuration = 1.5f;
        [SerializeField] private float strafeAfterRetreat = 2f;

        [Header("Defense")]
        [SerializeField] private float blockChanceStrafe = 0.05f;
        [SerializeField] private float blockChanceAttack = 0f;

        private vSimpleMeleeAI_Controller aiController;
        private UnityEngine.AI.NavMeshAgent agent;
        private Invector.vHealthController healthController;
        private Transform playerTransform;

        private bool isRetreating;
        private float retreatTimer;
        private float originalSpeed;
        private int attacksLanded;

        private void Start()
        {
            aiController = GetComponent<vSimpleMeleeAI_Controller>();
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            healthController = GetComponent<Invector.vHealthController>();

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;

            if (agent != null) originalSpeed = agent.speed;

            ConfigureInvectorAI();

            // Hook into damage dealt to track combo
            var meleeManager = GetComponent<Invector.vMelee.vMeleeManager>();
            if (meleeManager != null)
                meleeManager.onDamageHit.AddListener(OnAttackLanded);
        }

        private void ConfigureInvectorAI()
        {
            if (aiController == null) return;

            // Fast chase
            aiController.chaseSpeed = chaseSpeed;

            // Quick attacks
            aiController.maxAttackCount = attackComboCount;
            aiController.minTimeToAttack = minTimeBetweenAttacks;
            aiController.maxTimeToAttack = maxTimeBetweenAttacks;

            // Low strafe distance — get in close
            aiController.strafeDistance = 2f;

            // Almost never block — aggro style
            aiController.chanceToBlockInStrafe = blockChanceStrafe;
            aiController.chanceToBlockAttack = blockChanceAttack;

            // Fast NavMesh speed
            if (agent != null)
                agent.speed = 6f;
        }

        private void OnAttackLanded(Invector.vMelee.vHitInfo hitInfo)
        {
            attacksLanded++;

            // After combo count, retreat
            if (attacksLanded >= attackComboCount && !isRetreating)
            {
                attacksLanded = 0;
                StartCoroutine(RetreatCoroutine());
            }
        }

        private void Update()
        {
            if (healthController != null && healthController.isDead) return;
            if (aiController == null || playerTransform == null) return;

            // Sprint when far from player
            if (!isRetreating && agent != null && agent.isOnNavMesh)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                if (dist > sprintToPlayerDistance)
                    agent.speed = 8f; // Sprint
                else
                    agent.speed = 6f; // Normal fast
            }
        }

        private IEnumerator RetreatCoroutine()
        {
            isRetreating = true;

            // Disengage — run away from player
            if (agent != null && agent.isOnNavMesh && playerTransform != null)
            {
                Vector3 retreatDir = (transform.position - playerTransform.position).normalized;
                Vector3 retreatTarget = transform.position + retreatDir * retreatDistance;

                // Snap to NavMesh
                UnityEngine.AI.NavMeshHit navHit;
                if (UnityEngine.AI.NavMesh.SamplePosition(retreatTarget, out navHit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                    retreatTarget = navHit.position;

                agent.speed = 7f;
                agent.SetDestination(retreatTarget);
            }

            yield return new WaitForSeconds(retreatDuration);

            // Brief strafe/circle before re-engaging
            if (aiController != null)
                aiController.strafeDistance = 4f; // Wider circle after retreat

            yield return new WaitForSeconds(strafeAfterRetreat);

            // Re-engage
            if (aiController != null)
                aiController.strafeDistance = 2f; // Back to close strafe

            isRetreating = false;
        }

        private void OnDestroy()
        {
            var meleeManager = GetComponent<Invector.vMelee.vMeleeManager>();
            if (meleeManager != null)
                meleeManager.onDamageHit.RemoveListener(OnAttackLanded);
        }
    }
}
