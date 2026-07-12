using UnityEngine;

namespace Orpaits.Platforms
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public abstract class BasePlatform : MonoBehaviour
    {
        [Header("Base Platform Settings")]
        [SerializeField] protected Collider2D platformCollider;
        [SerializeField] protected SpriteRenderer spriteRenderer;

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

        protected virtual void OnPlayerEnter(Collision2D collision) { }
        protected virtual void OnPlayerExit(Collision2D collision) { }
        public virtual void ResetPlatform() { }
    }
}
