using UnityEngine;
using System;

/// <summary>
/// UltimateSystem — adrenaline meter, Adrenaline Rush + Elemental Burst.
/// Attach to player root.
/// </summary>
public class UltimateSystem : MonoBehaviour
{
    [Header("Adrenaline")]
    public float maxAdrenaline = 100f;
    public float adrenalinePerHit = 2f;
    public float adrenalinePerKill = 5f;
    public float adrenalinePerFinisher = 10f;

    [Header("Ultimate")]
    public float ultimateDuration = 8f;
    public float damageMultiplier = 1.5f;
    public float attackSpeedMultiplier = 1.3f;
    public float timeSlowScale = 0.5f;

    [Header("Elemental Burst")]
    public float fireBurstDamage = 60f;
    public float fireBurstRadius = 8f;
    public float lightningBurstDamage = 40f;
    public float lightningBurstStunDuration = 2f;
    public LayerMask enemyLayer;

    // State
    public float CurrentAdrenaline { get; private set; }
    public bool IsUltimateActive { get; private set; }

    // Events
    public event Action<float, float> OnAdrenalineChanged; // (current, max)
    public event Action OnUltimateActivated;
    public event Action OnUltimateEnded;

    private float ultimateTimer;
    private ElementSystem elementSystem;

    void Start()
    {
        elementSystem = GetComponent<ElementSystem>();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        if (IsUltimateActive)
        {
            ultimateTimer -= Time.unscaledDeltaTime; // Use unscaled since we slow time
            if (ultimateTimer <= 0f)
                EndUltimate();
        }

        // Activate ultimate: V key
        if (Input.GetKeyDown(KeyCode.V) && CurrentAdrenaline >= maxAdrenaline && !IsUltimateActive)
            ActivateUltimate();
    }

    public void AddAdrenaline(float amount)
    {
        if (IsUltimateActive) return;
        CurrentAdrenaline = Mathf.Min(maxAdrenaline, CurrentAdrenaline + amount);
        OnAdrenalineChanged?.Invoke(CurrentAdrenaline, maxAdrenaline);
    }

    public void AddKillAdrenaline() => AddAdrenaline(adrenalinePerKill);
    public void AddFinisherAdrenaline() => AddAdrenaline(adrenalinePerFinisher);

    void ActivateUltimate()
    {
        IsUltimateActive = true;
        ultimateTimer = ultimateDuration;
        CurrentAdrenaline = 0f;

        // Slow time but keep player at normal speed
        Time.timeScale = timeSlowScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        OnUltimateActivated?.Invoke();
        OnAdrenalineChanged?.Invoke(CurrentAdrenaline, maxAdrenaline);
        Debug.Log("ADRENALINE RUSH activated!");
    }

    void EndUltimate()
    {
        // Elemental Burst at the end
        PerformElementalBurst();

        IsUltimateActive = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        OnUltimateEnded?.Invoke();
        Debug.Log("Adrenaline Rush ended.");
    }

    void PerformElementalBurst()
    {
        if (elementSystem == null) return;

        ElementType element = elementSystem.ActiveElement;
        Debug.Log($"ELEMENTAL BURST: {element}!");

        switch (element)
        {
            case ElementType.Fire:
                // Ring of fire — damage all enemies in radius
                Collider[] fireHits = Physics.OverlapSphere(transform.position, fireBurstRadius, enemyLayer);
                foreach (var hit in fireHits)
                {
                    var health = hit.GetComponent<EnemyHealth>();
                    health?.TakeDamage(fireBurstDamage, ElementType.Fire);
                }
                break;

            case ElementType.Lightning:
                // Chain lightning — hit all enemies on screen (use large radius as approximation)
                Collider[] lightningHits = Physics.OverlapSphere(transform.position, 25f, enemyLayer);
                foreach (var hit in lightningHits)
                {
                    var health = hit.GetComponent<EnemyHealth>();
                    health?.TakeDamage(lightningBurstDamage, ElementType.Lightning);
                    var ai = hit.GetComponent<EnemyAI>();
                    ai?.ApplyStun(lightningBurstStunDuration);
                }
                break;
        }
    }

    /// <summary>
    /// Get current damage multiplier (for PlayerCombat to check).
    /// </summary>
    public float GetDamageMultiplier()
    {
        return IsUltimateActive ? damageMultiplier : 1f;
    }
}
