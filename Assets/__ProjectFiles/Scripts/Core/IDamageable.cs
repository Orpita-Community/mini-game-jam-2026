namespace Orpaits.Core
{
    /// <summary>
    /// Interface for any entity that can receive damage and healing.
    /// Implemented by the player's Health system, enemies, etc.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>Current health value.</summary>
        float CurrentHealth { get; }

        /// <summary>Maximum health value.</summary>
        float MaxHealth { get; }

        /// <summary>Whether the entity is dead.</summary>
        bool IsDead { get; }

        /// <summary>Apply damage. Returns true if damage was applied.</summary>
        bool TakeDamage(float amount);

        /// <summary>Apply healing. Returns true if healing was applied.</summary>
        bool Heal(float amount);
    }
}
