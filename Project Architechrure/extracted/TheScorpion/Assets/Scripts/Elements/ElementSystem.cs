using UnityEngine;
using System;

public enum ElementType { None, Fire, Lightning, Wind, Water }

/// <summary>
/// ElementSystem — element switching, abilities, energy management.
/// MVP: Fire + Lightning only. Wind/Water stubs for post-MVP.
/// Attach to player root.
/// </summary>
public class ElementSystem : MonoBehaviour
{
    [Header("Energy")]
    public float maxEnergy = 100f;
    public float energyRegenRate = 3f; // per second

    [Header("Fire Abilities")]
    public float fireTornadoDamage = 15f;
    public float fireTornadoRadius = 4f;
    public float fireTornadoDuration = 3f;
    public float fireTornadoCooldown = 8f;
    public float fireTornadoCost = 40f;

    public float fireAuraDuration = 6f;
    public float fireAuraBurnDamage = 5f;
    public float fireAuraCooldown = 12f;
    public float fireAuraCost = 30f;

    [Header("Lightning Abilities")]
    public float lightningBurstDamage = 20f;
    public float lightningBurstRadius = 3f;
    public float lightningBurstStunDuration = 1.5f;
    public float lightningBurstCooldown = 6f;
    public float lightningBurstCost = 35f;

    public float lightningSpeedDuration = 5f;
    public float lightningSpeedAtkBoost = 0.4f;
    public float lightningSpeedMoveBoost = 0.25f;
    public float lightningSpeedCooldown = 15f;
    public float lightningSpeedCost = 50f;

    public LayerMask enemyLayer;

    // State
    public ElementType ActiveElement { get; private set; } = ElementType.Fire;
    public float CurrentEnergy { get; private set; }
    public float Ability1CooldownRemaining { get; private set; }
    public float Ability2CooldownRemaining { get; private set; }
    public bool IsFireAuraActive { get; private set; }
    public bool IsLightningSpeedActive { get; private set; }

    // Events
    public event Action<ElementType> OnElementChanged;
    public event Action<float, float> OnEnergyChanged; // (current, max)
    public event Action<int, float> OnAbilityCooldownChanged; // (slot 1 or 2, remaining)

    private float fireAuraTimer;
    private float lightningSpeedTimer;

    void Start()
    {
        CurrentEnergy = maxEnergy;
        OnEnergyChanged?.Invoke(CurrentEnergy, maxEnergy);
        OnElementChanged?.Invoke(ActiveElement);
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        RegenEnergy();
        UpdateCooldowns();
        UpdateBuffTimers();
        HandleInput();
    }

    void HandleInput()
    {
        // Element switching: Q = previous, E = next
        if (Input.GetKeyDown(KeyCode.Q))
            SwitchElement(-1);
        if (Input.GetKeyDown(KeyCode.E))
            SwitchElement(1);

        // Ability 1: F
        if (Input.GetKeyDown(KeyCode.F))
            UseAbility1();

        // Ability 2: R
        if (Input.GetKeyDown(KeyCode.R))
            UseAbility2();
    }

    void SwitchElement(int direction)
    {
        // MVP: toggle between Fire and Lightning
        if (ActiveElement == ElementType.Fire)
            ActiveElement = ElementType.Lightning;
        else
            ActiveElement = ElementType.Fire;

        OnElementChanged?.Invoke(ActiveElement);
    }

    #region Ability 1

    void UseAbility1()
    {
        if (Ability1CooldownRemaining > 0f) return;

        switch (ActiveElement)
        {
            case ElementType.Fire:
                if (TrySpendEnergy(fireTornadoCost))
                {
                    FireTornado();
                    Ability1CooldownRemaining = fireTornadoCooldown;
                }
                break;
            case ElementType.Lightning:
                if (TrySpendEnergy(lightningBurstCost))
                {
                    LightningBurst();
                    Ability1CooldownRemaining = lightningBurstCooldown;
                }
                break;
        }
    }

    void FireTornado()
    {
        Debug.Log("FIRE TORNADO activated!");
        // AoE damage over time around player
        // In a full implementation, spawn a VFX prefab and use a coroutine for ticks
        Collider[] enemies = Physics.OverlapSphere(transform.position, fireTornadoRadius, enemyLayer);
        foreach (var e in enemies)
        {
            var health = e.GetComponent<EnemyHealth>();
            health?.TakeDamage(fireTornadoDamage, ElementType.Fire);
            health?.ApplyBurn(fireTornadoDamage * 0.5f, fireTornadoDuration);
        }
    }

    void LightningBurst()
    {
        Debug.Log("LIGHTNING BURST activated!");
        Collider[] enemies = Physics.OverlapSphere(transform.position, lightningBurstRadius, enemyLayer);
        foreach (var e in enemies)
        {
            var health = e.GetComponent<EnemyHealth>();
            health?.TakeDamage(lightningBurstDamage, ElementType.Lightning);

            var ai = e.GetComponent<EnemyAI>();
            ai?.ApplyStun(lightningBurstStunDuration);
        }
    }

    #endregion

    #region Ability 2

    void UseAbility2()
    {
        if (Ability2CooldownRemaining > 0f) return;

        switch (ActiveElement)
        {
            case ElementType.Fire:
                if (TrySpendEnergy(fireAuraCost))
                {
                    ActivateFireAura();
                    Ability2CooldownRemaining = fireAuraCooldown;
                }
                break;
            case ElementType.Lightning:
                if (TrySpendEnergy(lightningSpeedCost))
                {
                    ActivateLightningSpeed();
                    Ability2CooldownRemaining = lightningSpeedCooldown;
                }
                break;
        }
    }

    void ActivateFireAura()
    {
        Debug.Log("FIRE AURA activated!");
        IsFireAuraActive = true;
        fireAuraTimer = fireAuraDuration;
    }

    void ActivateLightningSpeed()
    {
        Debug.Log("LIGHTNING SPEED activated!");
        IsLightningSpeedActive = true;
        lightningSpeedTimer = lightningSpeedDuration;

        // Boost player speed
        var pc = GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.walkSpeed *= (1f + lightningSpeedMoveBoost);
            pc.sprintSpeed *= (1f + lightningSpeedMoveBoost);
        }
    }

    #endregion

    #region Buffs & Timers

    void UpdateBuffTimers()
    {
        if (IsFireAuraActive)
        {
            fireAuraTimer -= Time.deltaTime;
            if (fireAuraTimer <= 0f)
                IsFireAuraActive = false;
        }

        if (IsLightningSpeedActive)
        {
            lightningSpeedTimer -= Time.deltaTime;
            if (lightningSpeedTimer <= 0f)
            {
                IsLightningSpeedActive = false;
                // Revert speed boost
                var pc = GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.walkSpeed = 7f; // reset to default
                    pc.sprintSpeed = 11f;
                }
            }
        }
    }

    /// <summary>
    /// Returns bonus damage to add to melee attacks based on active buffs.
    /// Called by PlayerCombat.
    /// </summary>
    public float GetAttackBonusDamage()
    {
        if (IsFireAuraActive)
            return fireAuraBurnDamage;
        return 0f;
    }

    #endregion

    #region Energy

    void RegenEnergy()
    {
        if (CurrentEnergy < maxEnergy)
        {
            CurrentEnergy = Mathf.Min(maxEnergy, CurrentEnergy + energyRegenRate * Time.deltaTime);
            OnEnergyChanged?.Invoke(CurrentEnergy, maxEnergy);
        }
    }

    public void GainEnergy(float amount)
    {
        CurrentEnergy = Mathf.Min(maxEnergy, CurrentEnergy + amount);
        OnEnergyChanged?.Invoke(CurrentEnergy, maxEnergy);
    }

    bool TrySpendEnergy(float cost)
    {
        if (CurrentEnergy < cost) return false;
        CurrentEnergy -= cost;
        OnEnergyChanged?.Invoke(CurrentEnergy, maxEnergy);
        return true;
    }

    #endregion

    void UpdateCooldowns()
    {
        if (Ability1CooldownRemaining > 0f)
        {
            Ability1CooldownRemaining -= Time.deltaTime;
            OnAbilityCooldownChanged?.Invoke(1, Ability1CooldownRemaining);
        }
        if (Ability2CooldownRemaining > 0f)
        {
            Ability2CooldownRemaining -= Time.deltaTime;
            OnAbilityCooldownChanged?.Invoke(2, Ability2CooldownRemaining);
        }
    }
}
