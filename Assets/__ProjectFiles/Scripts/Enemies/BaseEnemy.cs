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
    public abstract class BaseEnemy : MonoBehaviour, IDamageable, IEnemyAudioSource
    {
        [Header("Health")]
        [SerializeField]
        protected float maxHealth = 1f;

        [SerializeField]
        protected float contactDamage = 1f;

        [SerializeField]
        [Tooltip("Seconds before contact can damage the same victim again. Collision callbacks " +
                 "fire every physics step, so 0 would drain the player's health almost instantly.")]
        protected float contactDamageInterval = 1f;

        [Header("Components")]
        [SerializeField]
        protected Collider2D enemyCollider;

        [SerializeField]
        protected SpriteRenderer spriteRenderer;

        [Header("Audio")]
        [SerializeField]
        protected AudioClip damageSfx;

        [SerializeField]
        protected AudioClip deathSfx;

        [SerializeField]
        protected AudioClip phaseTransitionSfx;

        /// <summary>Fired when health changes (current / max).</summary>
        public event Action<float, float> OnHealthChanged;

        /// <summary>
        /// Invoke OnHealthChanged from derived classes when force-setting health.
        /// </summary>
        protected void NotifyHealthChanged()
        {
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        /// <summary>Fired when the enemy takes damage.</summary>
        public event Action<float> OnDamageTaken;

        /// <summary>Fired when the enemy dies.</summary>
        public event Action OnDeath;

        public AudioClip DamageSfx => damageSfx;

        public AudioClip DeathSfx => deathSfx;

        public AudioClip PhaseTransitionSfx => phaseTransitionSfx;

        /// <summary>
        /// Current health value. Protected setter allows derived classes
        /// to force-set health for phase transitions and debug tools.
        /// External callers must use TakeDamage() / Heal().
        /// </summary>
        public float CurrentHealth { get; protected set; }

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

        protected virtual void OnEnable()
        {
            AudioManager.Instance?.RegisterEnemy(this);
        }

        protected virtual void OnDisable()
        {
            AudioManager.Instance?.UnregisterEnemy(this);
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

        // ── Contact damage ──────────────────────────────────────────────────

        private float lastContactDamageTime = float.NegativeInfinity;

        protected virtual void OnCollisionStay2D(Collision2D collision)
        {
            TryContactDamage(collision.gameObject);
        }

        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            TryContactDamage(other.gameObject);
        }

        /// <summary>
        /// Damages an <see cref="IDamageable"/> we are touching, rate-limited by
        /// <see cref="contactDamageInterval"/>. Other enemies are ignored so bodies
        /// bumping in a crowd don't kill each other.
        /// </summary>
        protected void TryContactDamage(GameObject target)
        {
            if (IsDead || contactDamage <= 0f) return;
            if (Time.time - lastContactDamageTime < contactDamageInterval) return;

            if (!target.TryGetComponent<IDamageable>(out var damageable)) return;
            if (damageable.IsDead || damageable is BaseEnemy) return;

            if (damageable.TakeDamage(contactDamage))
                lastContactDamageTime = Time.time;
        }
    }
}
