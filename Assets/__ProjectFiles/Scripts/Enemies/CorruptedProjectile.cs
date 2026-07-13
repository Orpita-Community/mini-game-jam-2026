using System.Threading;
using Orpaits.Core;
using UnityEngine;

namespace Orpaits.Enemies
{
    /// <summary>
    /// A corrupted file projectile thrown by the Boss Virus during Phase 1 (The Spam).
    /// Moves continuously forward via transform.position (no physics).
    /// Uses object pooling.
    ///
    /// Design reference: level-design-260712_2153.md (Boss Fight — Phase 1)
    /// </summary>
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

        private CancellationTokenSource cts;
        private GameObjectPool ownerPool;

        public void AssignPool(GameObjectPool pool) => ownerPool = pool;

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

        private void Update()
        {
            if (enabled)
                transform.position += transform.right * (speed * Time.deltaTime);
        }

        private async Awaitable LifetimeAsync(CancellationToken ct)
        {
            await Awaitable.WaitForSecondsAsync(lifetime, ct);
            if (!ct.IsCancellationRequested)
                ReturnToPool();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<IDamageable>(out var damageable) && !damageable.IsDead)
            {
                damageable.TakeDamage(damageAmount);
                ReturnToPool();
                return;
            }

            if ((destroyOnLayers & (1 << other.gameObject.layer)) != 0)
                ReturnToPool();
        }

        private void ReturnToPool()
        {
            if (ownerPool != null)
                ownerPool.Return(gameObject);
            else
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            cts?.Cancel();
            cts?.Dispose();
        }
    }
}
