using UnityEngine;

namespace Orpaits.Platforms
{
    /// <summary>
    /// Abstract base class for all platform types in Orpaits.
    /// Provides common references and lifecycle hooks.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public abstract class BasePlatform : MonoBehaviour
    {
        [Header("Base Platform Settings")]
        [SerializeField]
        protected Collider2D platformCollider;

        [SerializeField]
        protected SpriteRenderer spriteRenderer;

        /// <summary>
        /// Is the player currently standing on this platform?
        /// </summary>
        public bool IsPlayerOnPlatform { get; protected set; }

        protected virtual void Awake()
        {
            if (platformCollider == null)
                platformCollider = GetComponent<Collider2D>();

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }

        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                IsPlayerOnPlatform = true;
                OnPlayerEnter(collision);
                
            }
        }

        protected virtual void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                IsPlayerOnPlatform = false;
                OnPlayerExit(collision);
            }
        }

        /// <summary>
        /// Called when the player first lands on this platform.
        /// </summary>
        protected virtual void OnPlayerEnter(Collision2D collision) { }

        /// <summary>
        /// Called when the player leaves this platform.
        /// </summary>
        protected virtual void OnPlayerExit(Collision2D collision) { }

        /// <summary>
        /// Resets the platform to its initial state.
        /// Called on game start or checkpoint respawn.
        /// </summary>
        public virtual void ResetPlatform() { }
    }
}
