using UnityEngine;

/// <summary>
/// Temporary damage receiver for testing the player combat system.
/// </summary>
public class TestDamageable : MonoBehaviour, IDamageable
{
    [SerializeField] private float health = 100f;

    public bool IsDead => health <= 0f;

    /// <summary>
    /// Applies damage to the test target.
    /// </summary>
    /// <param name="damage">The incoming damage.</param>
    public void TakeDamage(float damage)
    {
        if (IsDead)
        {
            return;
        }

        health = Mathf.Max(0f, health - damage);

        Debug.Log(
            $"{gameObject.name} received {damage} damage. " +
            $"Remaining health: {health}");

        if (IsDead)
        {
            Debug.Log($"{gameObject.name} has been defeated!");
        }
    }
}