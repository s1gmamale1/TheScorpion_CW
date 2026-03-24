using UnityEngine;

/// <summary>
/// PlayerCombat — light/heavy attacks, combo chaining, hit detection.
/// Attach to player root. Requires a child "HitBox" trigger collider.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Stats")]
    public float lightDamage = 10f;
    public float heavyDamage = 25f;
    public float finisherDamage = 35f;
    public float attackRange = 2.5f;
    public LayerMask enemyLayer;

    [Header("Combo")]
    public float comboWindowTime = 0.6f;
    public int finisherThreshold = 3;

    [Header("Stagger")]
    public float staggerDamageMult = 0.5f;
    public float staggerDuration = 1.5f;

    // State
    public int ComboCount { get; private set; }
    public bool IsAttacking { get; private set; }
    public bool IsStaggered { get; private set; }

    public delegate void OnComboChanged(int count);
    public event OnComboChanged ComboChanged;
    public delegate void OnHitLanded(float damage);
    public event OnHitLanded HitLanded;

    private float comboTimer;
    private float staggerTimer;
    private float attackCooldown;
    private Animator animator;
    private PlayerController playerController;
    private UltimateSystem ultimateSystem;

    // Animator hashes
    private static readonly int AnimLightAttack = Animator.StringToHash("LightAttack");
    private static readonly int AnimHeavyAttack = Animator.StringToHash("HeavyAttack");
    private static readonly int AnimComboCount = Animator.StringToHash("ComboCount");

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        playerController = GetComponent<PlayerController>();
        ultimateSystem = GetComponent<UltimateSystem>();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        UpdateTimers();

        if (IsStaggered || (playerController != null && playerController.IsDodging))
            return;

        HandleAttackInput();
    }

    void UpdateTimers()
    {
        // Combo window
        if (ComboCount > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
                ResetCombo();
        }

        // Stagger
        if (IsStaggered)
        {
            staggerTimer -= Time.deltaTime;
            if (staggerTimer <= 0f)
                IsStaggered = false;
        }

        // Attack cooldown
        if (attackCooldown > 0f)
            attackCooldown -= Time.deltaTime;
    }

    void HandleAttackInput()
    {
        if (attackCooldown > 0f) return;

        if (Input.GetMouseButtonDown(0)) // Light attack
        {
            PerformAttack(false);
        }
        else if (Input.GetMouseButtonDown(1)) // Heavy attack
        {
            PerformAttack(true);
        }
    }

    void PerformAttack(bool isHeavy)
    {
        IsAttacking = true;
        ComboCount++;
        comboTimer = comboWindowTime;

        // Determine damage
        float damage;
        if (ComboCount >= finisherThreshold)
        {
            damage = finisherDamage;
            // Reset combo after finisher
            Invoke(nameof(ResetCombo), 0.3f);
        }
        else
        {
            damage = isHeavy ? heavyDamage : lightDamage;
        }

        // Stagger reduces damage
        if (IsStaggered)
            damage *= staggerDamageMult;

        // Set cooldown based on attack type
        attackCooldown = isHeavy ? 0.7f : 0.3f;

        // Trigger animation
        animator?.SetTrigger(isHeavy ? AnimHeavyAttack : AnimLightAttack);
        animator?.SetInteger(AnimComboCount, ComboCount);

        ComboChanged?.Invoke(ComboCount);

        // Deal damage to enemies in range
        DealDamage(damage);

        // Brief lock on movement during attack
        if (playerController != null)
        {
            playerController.CanMove = false;
            Invoke(nameof(UnlockMovement), isHeavy ? 0.5f : 0.2f);
        }
    }

    void DealDamage(float damage)
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position + transform.forward * 1.2f,
            attackRange,
            enemyLayer
        );

        foreach (var hit in hits)
        {
            var enemyHealth = hit.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                // Get element bonus from ElementSystem
                var elementSystem = GetComponent<ElementSystem>();
                float elementBonus = 0f;
                ElementType activeElement = ElementType.None;

                if (elementSystem != null)
                {
                    activeElement = elementSystem.ActiveElement;
                    elementBonus = elementSystem.GetAttackBonusDamage();
                }

                float totalDamage = damage + elementBonus;
                enemyHealth.TakeDamage(totalDamage, activeElement);

                HitLanded?.Invoke(totalDamage);

                // Feed adrenaline
                ultimateSystem?.AddAdrenaline(2f);

                // Feed element energy
                elementSystem?.GainEnergy(5f);
            }
        }
    }

    public void ApplyStagger()
    {
        IsStaggered = true;
        staggerTimer = staggerDuration;
    }

    void ResetCombo()
    {
        ComboCount = 0;
        ComboChanged?.Invoke(0);
    }

    void UnlockMovement()
    {
        if (playerController != null)
            playerController.CanMove = true;
        IsAttacking = false;
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * 1.2f, attackRange);
    }
}
