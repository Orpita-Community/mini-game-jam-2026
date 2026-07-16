using UnityEngine;

namespace Orpaits.Environment
{
    /// <summary>
    /// A kill zone at the bottom of the map.
    /// When the player enters this trigger, they instantly die and respawn
    /// at the latest checkpoint with icons preserved.
    /// 
    /// Design reference: lose-and-win-conditions-260712_2137.md (Lose Condition A)
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class KillZone : MonoBehaviour
    {
        [Header("Kill Zone Settings")]
        [SerializeField]
        [Tooltip("Tags that trigger death on enter")]
        private string targetTag = "Player";

        [SerializeField]
        [Tooltip("Reference to the CheckpointManager for respawn handling")]
        private CheckpointManager checkpointManager;

        /// <summary>Fired when the player dies from this kill zone.</summary>
        public event System.Action OnPlayerKilled;

        private void Awake()
        {
            // Ensure collider is a trigger
            var collider = GetComponent<Collider2D>();
            collider.isTrigger = true;

            if (checkpointManager == null)
            {
                checkpointManager = FindFirstObjectByType<CheckpointManager>();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // 1. Handle Player Death
            if (other.CompareTag(targetTag))
            {
                KillPlayer();
                return;
            }

            // 2. Handle Enemy Death
            if (other.CompareTag("Enemy"))
            {
                if (other.TryGetComponent<Orpaits.Core.IDamageable>(out var enemy))
                {
                    // Instantly kill the enemy
                    enemy.TakeDamage(999f); 
                }
                return;
            }
        }

        /// <summary>
        /// Kills the player and triggers respawn at the latest checkpoint.
        /// </summary>
        public void KillPlayer()
        {
            Debug.Log("this.gameobject [KillZone] Player fell into kill zone!");

            OnPlayerKilled?.Invoke();

            if (checkpointManager != null)
            {
                checkpointManager.RespawnAtCheckpoint();
            }
            else
            {
                Debug.LogError("[KillZone] No CheckpointManager found in scene!");
            }

            // TODO: Play death SFX via audio system when implemented
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                Gizmos.DrawCube(collider.bounds.center, collider.bounds.size);
            }
        }
    }
}
