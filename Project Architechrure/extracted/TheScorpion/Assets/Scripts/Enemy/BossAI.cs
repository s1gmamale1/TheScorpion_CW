using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// BossAI — The Fallen Guardian. 3 phase boss with unique attacks.
/// Attach to boss prefab INSTEAD of EnemyAI. Still requires NavMeshAgent + EnemyHealth.
/// Set EnemyHealth maxHealth to 300.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class BossAI : MonoBehaviour
{
    public enum BossPhase { Phase1, Phase2, Phase3 }
    public enum BossState { Idle, Chase, MeleeCombo, DashAttack, GroundSlam, SpinSlash, Summon, Stunned }

    [Header("References")]
    public GameObject basicEnemyPrefab;
    public GameObject fastEnemyPrefab;
    public Transform[] summonPoints;

    [Header("Stats")]
    public float baseMoveSpeed = 4f;
    public float attackRange = 2.5f;
    public float comboDamage = 15f;
    public float dashDamage = 20f;
    public float slamDamage = 25f;
    public float spinDamage = 30f;
    public float fireAuraDPS = 5f;
    public float contactDamageRange = 1.5f;

    // State
    public BossPhase CurrentPhase { get; private set; } = BossPhase.Phase1;
    public BossState CurrentState { get; private set; } = BossState.Idle;

    private NavMeshAgent agent;
    private EnemyHealth health;
    private Animator animator;
    private Transform player;

    private float stateTimer;
    private float actionCooldown = 2f;
    private float actionTimer;
    private float summonTimer;
    private float contactDamageTimer;
    private bool hasDealtDamage;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<EnemyHealth>();
        animator = GetComponentInChildren<Animator>();

        agent.speed = baseMoveSpeed;

        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        health.OnDamaged += (_) => CheckPhaseTransition();
    }

    void Update()
    {
        if (health.IsDead) return;

        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            agent.isStopped = true;
            return;
        }

        UpdatePhaseEffects();

        switch (CurrentState)
        {
            case BossState.Idle:
            case BossState.Chase:
                UpdateChase();
                break;
            case BossState.MeleeCombo:
            case BossState.DashAttack:
            case BossState.GroundSlam:
            case BossState.SpinSlash:
                UpdateAttackState();
                break;
            case BossState.Stunned:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                    ChangeState(BossState.Chase);
                break;
        }
    }

    void CheckPhaseTransition()
    {
        float hpPercent = health.CurrentHealth / health.maxHealth;

        if (hpPercent <= 0.3f && CurrentPhase != BossPhase.Phase3)
        {
            CurrentPhase = BossPhase.Phase3;
            agent.speed = baseMoveSpeed * 1.2f;
            Debug.Log("BOSS PHASE 3 — ENRAGED");
        }
        else if (hpPercent <= 0.6f && CurrentPhase == BossPhase.Phase1)
        {
            CurrentPhase = BossPhase.Phase2;
            Debug.Log("BOSS PHASE 2 — Fire Aura active");
        }
    }

    void UpdateChase()
    {
        if (player == null) return;

        agent.isStopped = false;
        agent.SetDestination(player.position);

        float dist = Vector3.Distance(transform.position, player.position);

        actionTimer -= Time.deltaTime;
        summonTimer -= Time.deltaTime;

        // Choose action when in range and cooldown ready
        if (actionTimer <= 0f)
        {
            if (dist <= attackRange)
                ChooseAttack();
            else if (dist > attackRange * 2f)
                ChangeState(BossState.DashAttack); // Gap close
        }

        // Summon minions on timer
        if (summonTimer <= 0f && CurrentPhase != BossPhase.Phase3)
        {
            SummonMinions();
            summonTimer = CurrentPhase == BossPhase.Phase1 ? 30f : 20f;
        }
    }

    void ChooseAttack()
    {
        float roll = Random.value;

        switch (CurrentPhase)
        {
            case BossPhase.Phase1:
                if (roll < 0.6f)
                    ChangeState(BossState.MeleeCombo);
                else
                    ChangeState(BossState.DashAttack);
                break;

            case BossPhase.Phase2:
                if (roll < 0.4f)
                    ChangeState(BossState.MeleeCombo);
                else if (roll < 0.7f)
                    ChangeState(BossState.GroundSlam);
                else
                    ChangeState(BossState.DashAttack);
                break;

            case BossPhase.Phase3:
                if (roll < 0.35f)
                    ChangeState(BossState.MeleeCombo);
                else if (roll < 0.6f)
                    ChangeState(BossState.SpinSlash);
                else if (roll < 0.8f)
                    ChangeState(BossState.GroundSlam);
                else
                    ChangeState(BossState.DashAttack);
                break;
        }
    }

    void ChangeState(BossState newState)
    {
        CurrentState = newState;
        hasDealtDamage = false;
        agent.isStopped = true;

        float speedMult = CurrentPhase == BossPhase.Phase3 ? 0.7f : 1f; // Phase 3 = faster attacks

        switch (newState)
        {
            case BossState.MeleeCombo:
                stateTimer = 1.8f * speedMult;
                animator?.SetTrigger("Attack");
                break;
            case BossState.DashAttack:
                stateTimer = 1.2f * speedMult;
                animator?.SetTrigger("DashAttack");
                // Dash toward player
                if (player != null)
                {
                    agent.isStopped = false;
                    agent.speed = baseMoveSpeed * 3f;
                    agent.SetDestination(player.position);
                }
                break;
            case BossState.GroundSlam:
                stateTimer = 2f * speedMult;
                animator?.SetTrigger("Slam");
                break;
            case BossState.SpinSlash:
                stateTimer = 1.5f * speedMult;
                animator?.SetTrigger("Spin");
                break;
            case BossState.Chase:
                agent.isStopped = false;
                agent.speed = baseMoveSpeed * (CurrentPhase == BossPhase.Phase3 ? 1.2f : 1f);
                actionTimer = actionCooldown * (CurrentPhase == BossPhase.Phase3 ? 0.7f : 1f);
                break;
        }
    }

    void UpdateAttackState()
    {
        // Face player during attacks
        if (player != null)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        stateTimer -= Time.deltaTime;

        // Deal damage at midpoint of attack
        if (!hasDealtDamage && stateTimer <= GetAttackDuration() * 0.5f)
        {
            PerformAttackDamage();
            hasDealtDamage = true;
        }

        if (stateTimer <= 0f)
        {
            agent.speed = baseMoveSpeed;
            ChangeState(BossState.Chase);
        }
    }

    float GetAttackDuration()
    {
        switch (CurrentState)
        {
            case BossState.MeleeCombo: return 1.8f;
            case BossState.DashAttack: return 1.2f;
            case BossState.GroundSlam: return 2f;
            case BossState.SpinSlash: return 1.5f;
            default: return 1f;
        }
    }

    void PerformAttackDamage()
    {
        if (player == null) return;
        float dist = Vector3.Distance(transform.position, player.position);

        float damage = 0f;
        float range = attackRange;

        switch (CurrentState)
        {
            case BossState.MeleeCombo:
                damage = comboDamage;
                range = attackRange;
                break;
            case BossState.DashAttack:
                damage = dashDamage;
                range = attackRange * 1.5f;
                break;
            case BossState.GroundSlam:
                damage = slamDamage;
                range = attackRange * 2f; // AoE shockwave
                break;
            case BossState.SpinSlash:
                damage = spinDamage;
                range = attackRange * 1.8f; // 360° range
                break;
        }

        // Phase 3 damage boost
        if (CurrentPhase == BossPhase.Phase3)
            damage *= 1.2f;

        if (dist <= range)
        {
            var playerHealth = player.GetComponent<PlayerHealth>();
            playerHealth?.TakeDamage(damage);
        }
    }

    void UpdatePhaseEffects()
    {
        // Phase 2+: Fire aura contact damage
        if (CurrentPhase >= BossPhase.Phase2 && player != null)
        {
            contactDamageTimer -= Time.deltaTime;
            if (contactDamageTimer <= 0f)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist <= contactDamageRange)
                {
                    var playerHealth = player.GetComponent<PlayerHealth>();
                    playerHealth?.TakeDamage(fireAuraDPS);
                }
                contactDamageTimer = 1f;
            }
        }
    }

    void SummonMinions()
    {
        if (summonPoints == null || summonPoints.Length == 0) return;

        Debug.Log("Boss summons minions!");

        switch (CurrentPhase)
        {
            case BossPhase.Phase1:
                // 2 Basic Monks
                for (int i = 0; i < 2 && i < summonPoints.Length; i++)
                {
                    if (basicEnemyPrefab != null)
                        Instantiate(basicEnemyPrefab, summonPoints[i].position, Quaternion.identity);
                }
                break;

            case BossPhase.Phase2:
                // 1 Fast Acolyte
                if (fastEnemyPrefab != null && summonPoints.Length > 0)
                    Instantiate(fastEnemyPrefab, summonPoints[0].position, Quaternion.identity);
                break;
        }
    }

    public void ApplyStun(float duration)
    {
        stateTimer = duration;
        ChangeState(BossState.Stunned);
    }
}
