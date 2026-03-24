using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// EnemyAI — NavMesh-based state machine: Idle → Chase → Attack → Retreat.
/// Supports stun. Configure behavior per enemy type via inspector fields.
/// Requires: NavMeshAgent component + baked NavMesh in scene.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour
{
    public enum EnemyType { Basic, Fast, Heavy, Boss }
    public enum AIState { Idle, Chase, Attack, Retreat, Stunned, Dead }

    [Header("Type")]
    public EnemyType enemyType = EnemyType.Basic;

    [Header("Detection")]
    public float detectionRange = 20f;
    public float attackRange = 2f;

    [Header("Attack")]
    public float attackDamage = 8f;
    public float attackWindup = 1f;    // Telegraph time before hit
    public float attackRecovery = 0.8f; // Time after attack before next action
    public float staggerChance = 0f;   // Chance to stagger player (Heavy = 0.3)

    [Header("Retreat (Fast enemy)")]
    public bool retreatsAfterAttack = false;
    public float retreatDistance = 6f;
    public float retreatDuration = 1f;

    // State
    public AIState CurrentState { get; private set; } = AIState.Idle;

    private NavMeshAgent agent;
    private EnemyHealth health;
    private Animator animator;
    private Transform player;

    private float stateTimer;
    private float stunTimer;
    private bool hasDealtDamageThisAttack;

    // Animator hashes
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimAttack = Animator.StringToHash("Attack");
    private static readonly int AnimStunned = Animator.StringToHash("Stunned");

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<EnemyHealth>();
        animator = GetComponentInChildren<Animator>();

        // Find player
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        health.OnDeath += OnDeath;

        // Configure NavMeshAgent speed based on type
        ConfigureForType();
    }

    void ConfigureForType()
    {
        switch (enemyType)
        {
            case EnemyType.Basic:
                agent.speed = 3f;
                attackDamage = 8f;
                attackWindup = 1f;
                attackRecovery = 0.8f;
                break;
            case EnemyType.Fast:
                agent.speed = 8f;
                attackDamage = 12f;
                attackWindup = 0.4f;
                attackRecovery = 0.5f;
                retreatsAfterAttack = true;
                break;
            case EnemyType.Heavy:
                agent.speed = 2f;
                attackDamage = 20f;
                attackWindup = 1.5f;
                attackRecovery = 1.2f;
                staggerChance = 0.3f;
                attackRange = 3f; // AoE slam
                break;
            case EnemyType.Boss:
                // Boss uses BossAI script instead; this is a fallback
                agent.speed = 4f;
                attackDamage = 15f;
                break;
        }
    }

    void Update()
    {
        if (health.IsDead) return;

        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            agent.isStopped = true;
            return;
        }

        // Update animator
        animator?.SetFloat(AnimSpeed, agent.velocity.magnitude);

        switch (CurrentState)
        {
            case AIState.Idle:
                UpdateIdle();
                break;
            case AIState.Chase:
                UpdateChase();
                break;
            case AIState.Attack:
                UpdateAttack();
                break;
            case AIState.Retreat:
                UpdateRetreat();
                break;
            case AIState.Stunned:
                UpdateStunned();
                break;
        }
    }

    #region State Updates

    void UpdateIdle()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= detectionRange)
            ChangeState(AIState.Chase);
    }

    void UpdateChase()
    {
        if (player == null) return;

        agent.isStopped = false;
        agent.SetDestination(player.position);

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= attackRange)
            ChangeState(AIState.Attack);
    }

    void UpdateAttack()
    {
        agent.isStopped = true;

        // Face player
        if (player != null)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        stateTimer -= Time.deltaTime;

        // Deal damage at the end of windup
        if (!hasDealtDamageThisAttack && stateTimer <= attackRecovery)
        {
            DealDamageToPlayer();
            hasDealtDamageThisAttack = true;
        }

        // Attack finished
        if (stateTimer <= 0f)
        {
            if (retreatsAfterAttack)
                ChangeState(AIState.Retreat);
            else
                ChangeState(AIState.Chase);
        }
    }

    void UpdateRetreat()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
            ChangeState(AIState.Chase);
    }

    void UpdateStunned()
    {
        agent.isStopped = true;
        stunTimer -= Time.deltaTime;

        if (stunTimer <= 0f)
        {
            animator?.SetBool(AnimStunned, false);
            ChangeState(AIState.Chase);
        }
    }

    #endregion

    void ChangeState(AIState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case AIState.Attack:
                stateTimer = attackWindup + attackRecovery;
                hasDealtDamageThisAttack = false;
                animator?.SetTrigger(AnimAttack);
                break;
            case AIState.Retreat:
                stateTimer = retreatDuration;
                // Move away from player
                if (player != null)
                {
                    Vector3 retreatDir = (transform.position - player.position).normalized;
                    Vector3 retreatTarget = transform.position + retreatDir * retreatDistance;
                    agent.isStopped = false;
                    agent.SetDestination(retreatTarget);
                }
                break;
        }
    }

    void DealDamageToPlayer()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > attackRange * 1.5f) return; // Player dodged out

        var playerHealth = player.GetComponent<PlayerHealth>();
        playerHealth?.TakeDamage(attackDamage);

        // Stagger chance (Heavy enemies)
        if (staggerChance > 0f && Random.value <= staggerChance)
        {
            var combat = player.GetComponent<PlayerCombat>();
            combat?.ApplyStagger();
        }
    }

    public void ApplyStun(float duration)
    {
        stunTimer = duration;
        ChangeState(AIState.Stunned);
        animator?.SetBool(AnimStunned, true);
    }

    void OnDeath()
    {
        CurrentState = AIState.Dead;
        agent.isStopped = true;
        agent.enabled = false;
    }

    // Debug
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
