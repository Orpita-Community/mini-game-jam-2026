using System.Threading;
using Orpaits.Core;
using UnityEngine;

namespace Orpaits.Enemies
{
    /// <summary>
    /// A corrupted file projectile thrown by the Boss Virus during Phase 1 (The Spam).
    /// Moves in a direction toward the player and damages on contact.
    ///
    /// Design reference: level-design-260712_2153.md (Boss Fight — Phase 1)
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CorruptedProjectile : MonoBehaviour
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

        private Vector2 direction;
        private CancellationTokenSource cts;

        /// <summary>
        /// Fire the projectile in the given direction.
        /// </summary>
        public void Launch(Vector2 dir)
        {
            direction = dir.normalized;
            cts = new CancellationTokenSource();
            _ = LifetimeAsync(cts.Token);
        }

        private void Update()
        {
            if (enabled)
                transform.Translate(direction * speed * Time.deltaTime);
        }

        private async Awaitable LifetimeAsync(CancellationToken ct)
        {
            await Awaitable.WaitForSecondsAsync(lifetime);
            if (!ct.IsCancellationRequested)
                Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Damage the player
            if (other.TryGetComponent<IDamageable>(out var damageable)
                && !damageable.IsDead)
            {
                damageable.TakeDamage(damageAmount);
                Destroy(gameObject);
                return;
            }

            // Destroy on hitting platforms/walls (via layer mask)
            if ((destroyOnLayers & (1 << other.gameObject.layer)) != 0)
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
