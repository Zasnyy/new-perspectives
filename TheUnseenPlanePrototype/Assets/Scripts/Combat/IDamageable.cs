/// <summary>
/// Defines the damage contract used by the combat system.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Gets whether this target can no longer receive damage.
    /// </summary>
    bool IsDead { get; }

    /// <summary>
    /// Applies damage to this target.
    /// </summary>
    /// <param name="damage">The amount of damage to apply.</param>
    void TakeDamage(float damage);
}