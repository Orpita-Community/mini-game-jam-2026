using System;
using System.Threading;
using UnityEngine;

namespace Orpaits.Enemies
{
    /// <summary>
    /// A virus that patrols a set path and damages the player on contact.
    /// Supports two patrol modes: Waypoints (transform list) or Range (left/right bounds).
    /// Destroyed by a single Data Disc projectile hit.
    ///
    /// Design reference: mechanics-260712_2137.md §8 (Virus Enemy),
    /// level-design-260712_2153.md (Viruses section)
    /// </summary>
    public class PatrolVirus : BaseEnemy
    {
        public enum PatrolMode
        {
            /// <summary>Patrol between two horizontal bounds.</summary>
            Range,

            /// <summary>Follow an ordered list of waypoint transforms.</summary>
            Waypoints
        }

        [Header("Patrol Settings")]
        [SerializeField]
        private PatrolMode patrolMode = PatrolMode.Range;

        [SerializeField]
        [Tooltip("Movement speed (world units/second)")]
        private float moveSpeed = 2f;

        [Header("Range Mode")]
        [SerializeField]
        [Tooltip("Left boundary for Range mode (world X)")]
        private float leftBound = -5f;

        [SerializeField]
        [Tooltip("Right boundary for Range mode (world X)")]
        private float rightBound = 5f;

        [Header("Waypoint Mode")]
        [SerializeField]
        [Tooltip("Ordered waypoints the virus patrols between")]
        private Transform[] waypoints;

        [Header("Visual")]
        [SerializeField]
        [Tooltip("Sprite to flip based on movement direction")]
        private bool flipSpriteOnDirection = true;

        /// <summary>Fired when the virus changes patrol direction.</summary>
        public event Action<bool> OnDirectionChanged;

        private Vector2 startPosition;
        private int currentWaypointIndex;
        private bool movingRight = true;
        private CancellationTokenSource cts;

        protected override void Awake()
        {
            base.Awake();
            startPosition = transform.position;
        }

        private void Start()
        {
            cts = new CancellationTokenSource();
            _ = PatrolLoopAsync(cts.Token);
        }

        private async Awaitable PatrolLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && !IsDead)
            {
                float step = moveSpeed * Time.deltaTime;

                switch (patrolMode)
                {
                    case PatrolMode.Range:
                        PatrolRange(step);
                        break;

                    case PatrolMode.Waypoints:
                        PatrolWaypoints(step);
                        break;
                }

                await Awaitable.NextFrameAsync();
            }
        }

        private void PatrolRange(float step)
        {
            if (movingRight)
            {
                transform.Translate(Vector2.right * step);
                if (transform.position.x >= rightBound)
                {
                    movingRight = false;
                    OnDirectionChanged?.Invoke(false);
                    if (flipSpriteOnDirection) FlipSprite(false);
                }
            }
            else
            {
                transform.Translate(Vector2.left * step);
                if (transform.position.x <= leftBound)
                {
                    movingRight = true;
                    OnDirectionChanged?.Invoke(true);
                    if (flipSpriteOnDirection) FlipSprite(true);
                }
            }
        }

        private void PatrolWaypoints(float step)
        {
            if (waypoints == null || waypoints.Length == 0)
                return;

            Transform target = waypoints[currentWaypointIndex];
            if (target == null) return;

            Vector2 currentPos = transform.position;
            Vector2 targetPos = target.position;
            float distance = Vector2.Distance(currentPos, targetPos);

            if (distance < 0.05f)
            {
                // Reached waypoint, move to next
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                return;
            }

            Vector2 direction = (targetPos - currentPos).normalized;
            transform.Translate(direction * step);

            // Flip sprite based on horizontal direction
            if (flipSpriteOnDirection)
            {
                bool facingRight = direction.x > 0f;
                FlipSprite(facingRight);
            }
        }

        private void FlipSprite(bool facingRight)
        {
            Vector3 scale = transform.localScale;
            scale.x = facingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        protected virtual void OnCollisionStay2D(Collision2D collision)
        {
            if (IsDead) return;

            if (collision.gameObject.TryGetComponent<Core.IDamageable>(out var damageable)
                && !damageable.IsDead)
            {
                damageable.TakeDamage(contactDamage);
            }
        }

        public override void OnProjectileHit(float damage)
        {
            // Patrol viruses die in one hit from a Data Disc
            TakeDamage(maxHealth);
        }

        protected override void Die()
        {
            // Disable patrol loop
            cts?.Cancel();

            // Brief death effect before destroying
            spriteRenderer.enabled = false;
            base.Die();

            // Destroy the GameObject after a short delay
            Destroy(gameObject, 0.3f);
        }

        private void OnDestroy()
        {
            cts?.Cancel();
            cts?.Dispose();
        }

        private void OnDrawGizmosSelected()
        {
            if (patrolMode == PatrolMode.Range)
            {
                Gizmos.color = Color.red;
                Vector3 pos = transform.position;
                Gizmos.DrawLine(new Vector3(leftBound, pos.y - 0.5f), new Vector3(leftBound, pos.y + 0.5f));
                Gizmos.DrawLine(new Vector3(rightBound, pos.y - 0.5f), new Vector3(rightBound, pos.y + 0.5f));
                Gizmos.DrawLine(new Vector3(leftBound, pos.y), new Vector3(rightBound, pos.y));
            }
            else if (waypoints != null && waypoints.Length > 0)
            {
                Gizmos.color = Color.yellow;
                Vector3 prev = waypoints[0] != null ? waypoints[0].position : transform.position;
                foreach (var wp in waypoints)
                {
                    if (wp == null) continue;
                    Gizmos.DrawWireSphere(wp.position, 0.2f);
                    Gizmos.DrawLine(prev, wp.position);
                    prev = wp.position;
                }
            }
        }
    }
}
