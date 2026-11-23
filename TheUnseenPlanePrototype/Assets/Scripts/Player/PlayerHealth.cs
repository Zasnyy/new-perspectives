using System;
using UnityEngine;

/// <summary>
/// Tracks player health and handles damage, temporary invulnerability, and death.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float invulnerabilityDuration = 0.35f;

    private float currentHealth;
    private float nextDamageTime;
    private bool isDead;

    /// <summary>
    /// Gets the player's current health.
    /// </summary>
    public float CurrentHealth => currentHealth;

    /// <summary>
    /// Gets the player's maximum health.
    /// </summary>
    public float MaxHealth => maxHealth;

    /// <summary>
    /// Gets whether the player has been defeated.
    /// </summary>
    public bool IsDead => isDead;

    /// <summary>
    /// Raised whenever the player's health changes.
    /// </summary>
    public event Action<float, float> HealthChanged;

    /// <summary>
    /// Raised when the player's health reaches zero.
    /// </summary>
    public event Action Died;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Applies damage unless the player is dead or temporarily invulnerable.
    /// </summary>
    /// <param name="damage">The amount of health to remove.</param>
    public void TakeDamage(float damage)
    {
        if (isDead || damage <= 0f || Time.time < nextDamageTime)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - damage);

        nextDamageTime = Time.time +
            Mathf.Max(0f, invulnerabilityDuration);

        HealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log(
            $"Player received {damage} damage. " +
            $"Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// Stops player movement and combat after health reaches zero.
    /// </summary>
    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        PlayerMovementController movement =
            GetComponent<PlayerMovementController>();

        if (movement != null)
        {
            movement.SetMovementLocked(true);
        }

        PlayerCombat combat = GetComponent<PlayerCombat>();

        if (combat != null)
        {
            combat.SetCombatLocked(true);
        }

        Debug.Log("Player has been defeated.");

        Died?.Invoke();
    }
}