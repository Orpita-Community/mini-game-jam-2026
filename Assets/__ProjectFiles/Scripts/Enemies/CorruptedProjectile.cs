using System.Threading;
using Orpaits.Core;
using UnityEngine;

namespace Orpaits.Enemies
{
    /// <summary>
    /// A corrupted file projectile thrown by the Boss Virus during Phase 1 (The Spam).
    /// Moves continuously forward (transform.right) until it hits something or its lifetime expires.
    /// Uses object pooling via GameObjectPool — overrides Destroy with pool return.
    ///
    /// Design reference: level-design-260712_2153.md (Boss Fight — Phase 1)
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CorruptedProjectile : MonoBehaviour, IPoolable
    {
        [Header("Movement")]
        [SerializeField]
        private float speed = 4f;

        [Header("Damage")]
        [SerializeField]
        private float damageAmount = 1f;

        [Header("Collision")]
        [SerializeField]
        private LayerMask destroyOnLayers = ~0;

        private CancellationTokenSource cts;
        private GameObjectPool ownerPool;

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
            cts?.Dispose();
            cts = new CancellationTokenSource();
            _ = LifetimeAsync(cts.Token);
        }

        public void OnPoolReturn()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        /// <summary>
        /// Each frame: move continuously in the projectile's forward direction.
        /// No direction parameter — just set the transform rotation on spawn.
        /// </summary>
        private void Update()
        {
            if (enabled)
                transform.Translate(speed * Time.deltaTime * Vector2.right);
        }

        private async Awaitable LifetimeAsync(CancellationToken ct)
        {
            await Awaitable.WaitForSecondsAsync(lifetime);
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

        private float lifetime = 5f;

        private void OnDestroy()
        {
            cts?.Cancel();
            cts?.Dispose();
        }
    }
}
