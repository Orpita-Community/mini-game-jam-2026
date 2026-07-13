using System;
using UnityEngine;

namespace Orpaits.Enemies
{
    /// <summary>
    /// A virus that patrols a set path and damages the player on contact.
    /// Supports two patrol modes: Waypoints (transform list) or Range (left/right bounds).
    /// Destroyed by a single Data Disc projectile hit.
    ///
    /// Movement uses Rigidbody2D.velocity in FixedUpdate for smooth physics-based
    /// motion with proper collision response and interpolation support.
    ///
    /// Design reference: mechanics-260712_2137.md §8 (Virus Enemy),
    /// level-design-260712_2153.md (Viruses section)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
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

        [Header("Physics")]
        [SerializeField]
        private Rigidbody2D rb;

        [Header("Visual")]
        [SerializeField]
        [Tooltip("Sprite to flip based on movement direction")]
        private bool flipSpriteOnDirection = true;

        /// <summary>Fired when the virus changes patrol direction.</summary>
        public event Action<bool> OnDirectionChanged;

        private Vector2 startPosition;
        private int currentWaypointIndex;
        private bool movingRight = true;

        protected override void Awake()
        {
            base.Awake();
            startPosition = transform.position;
            if (rb == null) rb = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            if (IsDead) return;

            Vector2 velocity = rb.velocity;
            velocity.y = rb.velocity.y; // preserve vertical (gravity etc.)

            switch (patrolMode)
            {
                case PatrolMode.Range:
                    velocity.x = PatrolRangeVelocity();
                    break;

                case PatrolMode.Waypoints:
                    velocity.x = PatrolWaypointsVelocity();
                    break;
            }

            rb.velocity = velocity;
        }

        // ── Range Mode ──────────────────────────────────────────────────────

        private float PatrolRangeVelocity()
        {
            if (movingRight)
            {
                if (transform.position.x >= rightBound)
                {
                    movingRight = false;
                    OnDirectionChanged?.Invoke(false);
                    if (flipSpriteOnDirection) FlipSprite(false);
                    return -moveSpeed;
                }
                return moveSpeed;
            }
            else
            {
                if (transform.position.x <= leftBound)
                {
                    movingRight = true;
                    OnDirectionChanged?.Invoke(true);
                    if (flipSpriteOnDirection) FlipSprite(true);
                    return moveSpeed;
                }
                return -moveSpeed;
            }
        }

        // ── Waypoint Mode ───────────────────────────────────────────────────

        private float PatrolWaypointsVelocity()
        {
            if (waypoints == null || waypoints.Length == 0)
                return 0f;

            Transform target = waypoints[currentWaypointIndex];
            if (target == null) return 0f;

            float distanceX = target.position.x - transform.position.x;

            if (Mathf.Abs(distanceX) < 0.1f)
            {
                // Reached waypoint, move to next
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                return 0f;
            }

            float direction = Mathf.Sign(distanceX);

            if (flipSpriteOnDirection)
                FlipSprite(direction > 0f);

            return direction * moveSpeed;
        }

        // ── Visual ──────────────────────────────────────────────────────────

        private void FlipSprite(bool facingRight)
        {
            Vector3 scale = transform.localScale;
            scale.x = facingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        // ── Combat ──────────────────────────────────────────────────────────

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
            rb.velocity = Vector2.zero;
            rb.simulated = false;
            spriteRenderer.enabled = false;
            base.Die();
            Destroy(gameObject, 0.3f);
        }

        // ── Gizmos ──────────────────────────────────────────────────────────

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
