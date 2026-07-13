using System;
using Orpaits.Core;
using UnityEngine;

namespace Orpaits.Enemies
{
    /// <summary>
    /// Abstract base class for all enemies in Orpaits.
    /// Provides health, contact damage, and death lifecycle.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public abstract class BaseEnemy : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField]
        protected float maxHealth = 1f;

        [SerializeField]
        protected float contactDamage = 1f;

        [Header("Components")]
        [SerializeField]
        protected Collider2D enemyCollider;

        [SerializeField]
        protected SpriteRenderer spriteRenderer;

        /// <summary>Fired when health changes (current / max).</summary>
        public event Action<float, float> OnHealthChanged;

        /// <summary>Fired when the enemy takes damage.</summary>
        public event Action<float> OnDamageTaken;

        /// <summary>Fired when the enemy dies.</summary>
        public event Action OnDeath;

        /// <summary>
        /// Current health value. Setting it clamps between 0 and maxHealth.
        /// </summary>
        public float CurrentHealth { get; private set; }

        public float MaxHealth => maxHealth;

        /// <summary>
        /// Normalized health (0..1) for UI bars.
        /// </summary>
        public float HealthNormalized => maxHealth > 0f ? CurrentHealth / maxHealth : 0f;

        public bool IsDead { get; protected set; }

        protected virtual void Awake()
        {
            if (enemyCollider == null)
                enemyCollider = GetComponent<Collider2D>();

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            CurrentHealth = maxHealth;
        }

        /// <summary>
        /// Heal the enemy by the given amount.
        /// </summary>
        public virtual bool Heal(float amount)
        {
            if (IsDead || amount <= 0f)
                return false;

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            return true;
        }

        /// <summary>
        /// Apply damage to this enemy.
        /// </summary>
        /// <param name="amount">Damage amount.</param>
        /// <returns>True if the damage was applied, false if already dead.</returns>
        public virtual bool TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f)
                return false;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            OnDamageTaken?.Invoke(amount);

            if (CurrentHealth <= 0f)
                Die();

            return true;
        }

        /// <summary>
        /// Handle enemy death. Disables collider, plays death effects.
        /// Override for custom death behavior (e.g., boss phase transition).
        /// </summary>
        protected virtual void Die()
        {
            if (IsDead) return;
            IsDead = true;

            enemyCollider.enabled = false;
            OnDeath?.Invoke();
        }

        /// <summary>
        /// Reset enemy to full health and alive state.
        /// </summary>
        public virtual void ResetEnemy()
        {
            IsDead = false;
            CurrentHealth = maxHealth;
            enemyCollider.enabled = true;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        /// <summary>
        /// Called when the player's projectile/disc collides with this enemy.
        /// Override to add per-enemy projectile reactions.
        /// </summary>
        public virtual void OnProjectileHit(float damage)
        {
            TakeDamage(damage);
        }
    }
}
