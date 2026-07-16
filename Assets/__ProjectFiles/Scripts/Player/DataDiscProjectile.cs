using System.Threading;
using Orpaits.Core;
using Orpaits.Enemies;
using UnityEngine;

namespace Orpaits.Player
{
    /// <summary>
    /// The player's CD projectile. Flies straight up when thrown.
    /// Damages enemies and returns to the pool.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DataDiscProjectile : MonoBehaviour, IPoolable
    {
        [Header("Movement")]
        [SerializeField] private float speed = 15f;
        [SerializeField] private float lifetime = 3f;

        [Header("Damage")]
        [SerializeField] private float damageAmount = 1f;

        [Header("Collision")]
        [SerializeField] [Tooltip("Layers that instantly destroy the CD (like walls/ceilings)")]
        private LayerMask destroyOnLayers;

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

        private async Awaitable MoveAndLifetimeAsync(CancellationToken ct)
        {
            float elapsed = 0f;

            while (elapsed < lifetime && !ct.IsCancellationRequested)
            {
                // Always fly straight UP relative to the world
                transform.position += Vector3.up * (speed * Time.deltaTime);
                
                // Add a cool spinning effect for the CD!
                transform.Rotate(0, 0, -1000f * Time.deltaTime);
                
                elapsed += Time.deltaTime;
                await Awaitable.NextFrameAsync(ct);
            }

            if (!ct.IsCancellationRequested)
                ReturnToPool();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Did we hit a Boss or a Patrol Virus?
            if (other.TryGetComponent<BaseEnemy>(out var enemy) && !enemy.IsDead)
            {
                enemy.OnProjectileHit(damageAmount);
                ReturnToPool();
                return;
            }

            // Did we hit a wall/ceiling?
            if ((destroyOnLayers & (1 << other.gameObject.layer)) != 0)
            {
                ReturnToPool();
            }
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