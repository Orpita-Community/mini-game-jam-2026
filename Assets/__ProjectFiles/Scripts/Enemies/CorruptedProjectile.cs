using System.Threading;
using Orpaits.Core;
using UnityEngine;

namespace Orpaits.Enemies
{
    /// <summary>
    /// A corrupted file projectile thrown by the Boss Virus during Phase 1 (The Spam).
    /// Moves continuously via transform.position += transform.up in an async loop.
    /// No Update — the spawn point sets the rotation, and the projectile moves in
    /// its own "up" direction every frame.
    ///
    /// Requires a kinematic Rigidbody2D: the collider is moved by transform, and
    /// without a body it would be a static collider, which only reports triggers
    /// while the other body happens to be awake — a sleeping player would be missed.
    ///
    /// Design reference: level-design-260712_2153.md (Boss Fight — Phase 1)
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
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
            _ = MoveAndLifetimeAsync(cts.Token);
        }

        public void OnPoolReturn()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        /// <summary>
        /// Drives movement every frame in transform.up direction.
        /// Also returns to pool after lifetime expires.
        /// The spawner (BossVirus) sets the projectile's rotation to match the
        /// spawn point — so transform.up equals the spawn point's up vector.
        /// </summary>
        private async Awaitable MoveAndLifetimeAsync(CancellationToken ct)
        {
            float elapsed = 0f;

            while (elapsed < lifetime && !ct.IsCancellationRequested)
            {
                transform.position += transform.up * (speed * Time.deltaTime);
                elapsed += Time.deltaTime;
                await Awaitable.NextFrameAsync(ct);
            }

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
