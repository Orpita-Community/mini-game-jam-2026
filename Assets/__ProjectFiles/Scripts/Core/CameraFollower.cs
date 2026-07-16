using UnityEngine;

namespace Orpaits.Core
{
    /// <summary>
    /// Camera follows the player vertically (up AND down) as they climb through
    /// the OS levels. The camera is locked horizontally — it never moves left or
    /// right with the player, only tracking on the Y axis.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollower : MonoBehaviour
    {
        [Header("Follow Settings")]
        [SerializeField]
        [Tooltip("The target GameObject to follow (usually the Player)")]
        private Transform target;

        [SerializeField]
        [Tooltip("How smoothly the camera follows (higher = snappier)")]
        private float followSpeed = 5f;

        [SerializeField]
        [Tooltip("Vertical offset from target position")]
        private float yOffset = 2f;

        [SerializeField]
        [Tooltip("Fixed Z position of the camera")]
        private float zPosition = -10f;

        [Header("Vertical Constraints")]
        [SerializeField]
        [Tooltip("Lowest Y position the camera can go (level floor)")]
        private float minY = 0f;

        // The camera's X never changes — captured once at Awake.
        private float lockedX;
        private float currentY;

        private void Awake()
        {
            // Lock the horizontal position to wherever the camera starts in the scene.
            lockedX = transform.position.x;

            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    target = player.transform;
            }

            if (target != null)
            {
                currentY = Mathf.Max(target.position.y + yOffset, minY);
                transform.position = new Vector3(lockedX, currentY, zPosition);
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Smoothly follow the target on Y in BOTH directions (up and down).
            float targetY = Mathf.Max(target.position.y + yOffset, minY);
            currentY = Mathf.Lerp(currentY, targetY, followSpeed * Time.deltaTime);
            currentY = Mathf.Max(currentY, minY);

            // X stays constant — camera does not track the player horizontally.
            transform.position = new Vector3(lockedX, currentY, zPosition);
        }

        /// <summary>
        /// Sets the camera target. Called when player respawns or scene changes.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                currentY = Mathf.Max(target.position.y + yOffset, minY);
            }
        }

        /// <summary>
        /// Resets the camera Y tracking (e.g., on new zone or checkpoint respawn).
        /// Re-syncs the smoothed Y to the target so the camera snaps back to the player.
        /// </summary>
        public void ResetVerticalTracking()
        {
            if (target != null)
                currentY = Mathf.Max(target.position.y + yOffset, minY);
        }
    }
}
