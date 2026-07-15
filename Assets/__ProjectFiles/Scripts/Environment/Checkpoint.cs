using UnityEngine;

namespace Orpaits.Environment
{
    /// <summary>
    /// A checkpoint that saves the player's progress.
    /// When the player touches a checkpoint, it activates and becomes their
    /// respawn point upon death (fatal fall or health = 0).
    /// 
    /// Collected icons are always preserved on respawn.
    /// Only the most recently activated checkpoint is used.
    /// 
    /// Design reference: lose-and-win-conditions-260712_2137.md (Lose Condition A),
    /// level-design-260712_2153.md (Checkpoints section)
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [Header("Checkpoint Settings")]
        [SerializeField]
        [Tooltip("Checkpoint identifier for debugging")]
        private string checkpointID;

        [SerializeField]
        [Tooltip("Visual when inactive (default)")]
        private Sprite inactiveSprite;

        [SerializeField]
        [Tooltip("Visual when activated")]
        private Sprite activeSprite;

        [Header("References")]
        [SerializeField]
        private SpriteRenderer spriteRenderer;

        /// <summary>Fired when this checkpoint is activated.</summary>
        public event System.Action<Checkpoint> OnActivated;

        /// <summary>Is this checkpoint currently activated?</summary>
        public bool IsActivated { get; private set; }

        /// <summary>Unique identifier for this checkpoint.</summary>
        public string CheckpointID => checkpointID;

        /// <summary>Respawn position (slightly above the checkpoint).</summary>
        public Vector2 RespawnPosition => transform.position + Vector3.up * 0.5f;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsActivated) return;

            if (other.CompareTag("Player"))
            {
                Activate();
            }
        }

        /// <summary>
        /// Activates this checkpoint, making it the respawn point.
        /// </summary>
        public void Activate()
        {
            if (IsActivated) return;

            IsActivated = true;

            // Update visual
            if (activeSprite != null)
                spriteRenderer.sprite = activeSprite;

            OnActivated?.Invoke(this);
            Debug.Log($"[Checkpoint] Activated: {checkpointID} at {transform.position}");
        }

        /// <summary>
        /// Resets this checkpoint to inactive state.
        /// </summary>
        public void ResetCheckpoint()
        {
            IsActivated = false;

            if (inactiveSprite != null)
                spriteRenderer.sprite = inactiveSprite;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(1.5f, 0.5f, 0));
            Gizmos.DrawSphere(RespawnPosition, 0.15f);
        }
    }
}
