using UnityEngine;
using System;

/// <summary>
/// PlayerHealth — manages HP, taking damage, death.
/// Attach to player root alongside PlayerController.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    // Events for UI binding
    public event Action<float, float> OnHealthChanged; // (current, max)
    public event Action OnDeath;

    private PlayerController playerController;

    void Start()
    {
        CurrentHealth = maxHealth;
        playerController = GetComponent<PlayerController>();
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (IsDead) return;
        if (playerController != null && playerController.IsInvincible) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    void Die()
    {
        IsDead = true;
        OnDeath?.Invoke();

        // Disable player input
        if (playerController != null)
            playerController.CanMove = false;

        var animator = GetComponentInChildren<Animator>();
        animator?.SetTrigger("Death");

        // Notify GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();
    }
}
