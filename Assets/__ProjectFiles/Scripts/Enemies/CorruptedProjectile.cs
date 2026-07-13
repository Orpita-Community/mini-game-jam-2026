using System.Threading;
using Orpaits.Core;
using UnityEngine;

namespace Orpaits.Enemies
{
    /// <summary>
    /// A corrupted file projectile thrown by the Boss Virus during Phase 1 (The Spam).
    /// Moves continuously forward via Rigidbody2D.velocity — set once on spawn,
    /// physics handles the rest. Uses object pooling.
    ///
    /// Design reference: level-design-260712_2153.md (Boss Fight — Phase 1)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class CorruptedProjectile : MonoBehaviour, IPoolable
    {
        [Header("Movement")]
        [SerializeField]
        private float speed = 4f;

        [SerializeField]
        private float lifetime = 5f;

        [Header("Damage")]
        [SerializeField]
        private float damageAmount = 1f;

        [Header("Collision")]
        [SerializeField]
        private LayerMask destroyOnLayers = ~0;

        [Header("Physics")]
        [SerializeField]
        private Rigidbody2D rb;

        private CancellationTokenSource cts;
        private GameObjectPool ownerPool;

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
        }

        /// <summary>
        /// Assign the pool this projectile returns to.
        /// Called by BossVirus after Get().
        /// </summary>
        public void AssignPool(GameObjectPool pool)
        {
            ownerPool = pool;
        }

        public void OnPoolGet()
        {
            // Set velocity once — physics moves it forward continuously
            rb.velocity = transform.right * speed;

            cts?.Dispose();
            cts = new CancellationTokenSource();
            _ = LifetimeAsync(cts.Token);
        }

        public void OnPoolReturn()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;

            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        private async Awaitable LifetimeAsync(CancellationToken ct)
        {
            await Awaitable.WaitForSecondsAsync(lifetime, ct);
            if (!ct.IsCancellationRequested)
                ReturnToPool();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Damage the player
            if (other.TryGetComponent<IDamageable>(out var damageable)
                && !damageable.IsDead)
            {
                damageable.TakeDamage(damageAmount);
                ReturnToPool();
                return;
            }

            // Return to pool on hitting platforms/walls (via layer mask)
            if ((destroyOnLayers & (1 << other.gameObject.layer)) != 0)
            {
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            if (ownerPool != null)
            {
                ownerPool.Return(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            cts?.Cancel();
            cts?.Dispose();
        }
    }
}
