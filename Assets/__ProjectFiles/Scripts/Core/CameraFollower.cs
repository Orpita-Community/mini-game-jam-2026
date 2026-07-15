using UnityEngine;

namespace Orpaits.Core
{
    /// <summary>
    /// Camera follows the player vertically as they climb through the OS levels.
    /// The camera only moves upward, creating the feeling of a continuous climb.
    /// Horizontal movement is free within the view.
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
        [Tooltip("Offset from target position")]
        private Vector3 offset = new Vector3(0, 2, -10);

        [Header("Vertical Constraints")]
        [SerializeField]
        [Tooltip("Lowest Y position the camera can go")]
        private float minY = 0f;

        [SerializeField]
        [Tooltip("If true, camera only moves upward (never down)")]
        private bool lockUpwardProgression = true;

        private float currentY;

        private void Awake()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    target = player.transform;
            }

            if (target != null)
            {
                currentY = target.position.y + offset.y;
                transform.position = new Vector3(
                    target.position.x + offset.x,
                    Mathf.Max(target.position.y + offset.y, minY),
                    offset.z
                );
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float targetY = target.position.y + offset.y;

            if (lockUpwardProgression && targetY < currentY)
            {
                // Camera stays at highest point reached
                targetY = currentY;
            }

            // Smooth follow
            currentY = Mathf.Lerp(currentY, targetY, followSpeed * Time.deltaTime);

            // Clamp to min Y
            currentY = Mathf.Max(currentY, minY);

            transform.position = new Vector3(
                target.position.x + offset.x,
                currentY,
                offset.z
            );
        }

        /// <summary>
        /// Sets the camera target. Called when player respawns or scene changes.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                currentY = target.position.y + offset.y;
            }
        }

        /// <summary>
        /// Resets the camera Y tracking (e.g., on new zone or checkpoint respawn).
        /// Allows camera to move down again if lockUpwardProgression is on.
        /// </summary>
        public void ResetVerticalTracking()
        {
            if (target != null)
                currentY = target.position.y + offset.y;
        }
    }
}
