using UnityEngine;
using Orpaits.Character;
using Orpaits.Core;

namespace Orpaits.Environment
{
    /// <summary>
    /// Tracks the most recently activated checkpoint and handles respawning
    /// the player to that checkpoint.
    /// 
    /// Collected icons are always preserved on respawn.
    /// 
    /// Design reference: lose-and-win-conditions-260712_2137.md (Lose Condition A),
    /// level-design-260712_2153.md (Checkpoints section)
    /// </summary>
    public class CheckpointManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        [Tooltip("The player GameObject")]
        private Transform player;

        [SerializeField]
        [Tooltip("The PlayerController component for resetting")]
        private PlayerController playerController;

        [Header("Respawn")]
        [SerializeField]
        [Tooltip("The CameraFollower to reset on respawn")]
        private CameraFollower cameraFollower;
        
        [Header("Checkpoint")]
        [SerializeField] private Checkpoint[] allCheckpoints;

        private Checkpoint activeCheckpoint;

        /// <summary>Currently active checkpoint, or null if none.</summary>
        public Checkpoint ActiveCheckpoint => activeCheckpoint;

        private void Awake()
        {
            if (player == null)
            {
                GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null)
                {
                    player = playerGO.transform;
                    playerController = playerGO.GetComponent<PlayerController>();
                }
            }

            if (cameraFollower == null)
            {
                cameraFollower = FindFirstObjectByType<CameraFollower>();
            }

            if (allCheckpoints == null)
            {
                allCheckpoints = FindObjectsOfType<Checkpoint>();
            }
            foreach (var cp in allCheckpoints)
            {
                cp.OnActivated += OnCheckpointActivated;
            }
        }

      
        private void OnCheckpointActivated(Checkpoint checkpoint)
        {
            activeCheckpoint = checkpoint;
            Debug.Log($"[CheckpointManager] New checkpoint saved: {checkpoint.CheckpointID} at {checkpoint.transform.position}");
        }

        /// <summary>
        /// Respawns the player at the latest checkpoint.
        /// Returns true if a checkpoint was available.
        /// </summary>
        public bool RespawnAtCheckpoint()
        {
            if (activeCheckpoint == null)
            {
                Debug.LogWarning("[CheckpointManager] No checkpoint set! Respawning at origin.");
                if (playerController != null)
                    playerController.ResetToPosition(Vector2.zero);

                // Snap camera back to player
                if (cameraFollower != null)
                    cameraFollower.ResetVerticalTracking();

                return false;
            }

            Debug.Log($"[CheckpointManager] Respawning at checkpoint: {activeCheckpoint.CheckpointID}");
            if (playerController != null)
                playerController.ResetToPosition(activeCheckpoint.RespawnPosition);

            // Snap camera back to player (cancels upward-progression lock)
            if (cameraFollower != null)
                cameraFollower.ResetVerticalTracking();

            return true;
        }

        /// <summary>
        /// Manually register the first checkpoint as active (for starting from a specific checkpoint).
        /// </summary>
        public void SetSpawnCheckpoint(Checkpoint checkpoint)
        {
            if (checkpoint != null)
            {
                // Activate without the trigger (for initial spawn)
                checkpoint.Activate();
            }
        }
    }
}
