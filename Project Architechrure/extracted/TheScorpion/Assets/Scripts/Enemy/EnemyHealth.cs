using UnityEngine;
using System;

/// <summary>
/// EnemyHealth — HP, taking damage, element reactions (burn, stun resistance).
/// Attach to each enemy prefab root.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 30f;

    [Header("Element Resistances (0 = immune, 1 = normal, 2 = weak)")]
    public float fireResistance = 1f;
    public float lightningResistance = 1f;
    public float lightAttackReduction = 1f; // Heavy enemies: 0.5

    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    public event Action OnDeath;
    public event Action<float> OnDamaged;

    // Burn state
    private bool isBurning;
    private float burnDamagePerTick;
    private float burnTimer;
    private float burnTickInterval = 0.5f;
    private float burnTickTimer;

    void Start()
    {
        CurrentHealth = maxHealth;
    }

    void Update()
    {
        if (IsDead) return;
        ProcessBurn();
    }

    public void TakeDamage(float damage, ElementType element = ElementType.None)
    {
        if (IsDead) return;

        float finalDamage = damage;

        // Apply element resistance
        switch (element)
        {
            case ElementType.Fire:
                finalDamage *= fireResistance;
                break;
            case ElementType.Lightning:
                finalDamage *= lightningResistance;
                break;
        }

        CurrentHealth -= finalDamage;
        OnDamaged?.Invoke(finalDamage);

        // Visual feedback — flash red (you can expand this with a shader)
        StartCoroutine(FlashDamage());

        if (CurrentHealth <= 0f)
            Die();
    }

    public void ApplyBurn(float damagePerSecond, float duration)
    {
        isBurning = true;
        burnDamagePerTick = damagePerSecond * burnTickInterval;
        burnTimer = duration;
        burnTickTimer = 0f;
    }

    void ProcessBurn()
    {
        if (!isBurning) return;

        burnTimer -= Time.deltaTime;
        burnTickTimer -= Time.deltaTime;

        if (burnTickTimer <= 0f)
        {
            TakeDamage(burnDamagePerTick, ElementType.None); // Burn is raw damage
            burnTickTimer = burnTickInterval;
        }

        if (burnTimer <= 0f)
            isBurning = false;
    }

    void Die()
    {
        IsDead = true;
        OnDeath?.Invoke();

        // Feed adrenaline to player
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var ultimate = player.GetComponent<UltimateSystem>();
            ultimate?.AddKillAdrenaline();
        }

        // Play death anim then destroy
        var animator = GetComponentInChildren<Animator>();
        animator?.SetTrigger("Death");

        // Disable collider immediately so player can't hit corpse
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 2f); // Delay for death animation
    }

    System.Collections.IEnumerator FlashDamage()
    {
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer == null) yield break;

        Color original = renderer.material.color;
        renderer.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        renderer.material.color = original;
    }
}
