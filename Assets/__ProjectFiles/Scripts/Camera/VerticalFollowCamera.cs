using UnityEngine;

namespace Orpaits.Core
{
    /// <summary>
    /// A camera that follows the player up and down, but features a constantly rising 
    /// "floor limit" to force the player upwards.
    /// </summary>
    public class VerticalFollowCamera : MonoBehaviour
    {
        [Header("Tracking Settings")]
        [SerializeField] private Transform target;
        [SerializeField] [Tooltip("How smoothly the camera catches up to the player")] 
        private float smoothTime = 0.2f;
        [SerializeField] [Tooltip("Vertical offset to keep the player lower on the screen")] 
        private float yOffset = 0f;

        [Header("Auto-Scroll Tension")]
        [SerializeField] [Tooltip("How fast the camera's minimum height rises (units per second)")]
        private float autoScrollSpeed = 1.5f;
        
        [SerializeField] [Tooltip("The maximum distance the player can fall back down before the camera refuses to track lower.")]
        private float maxFallDistance = 10f;

        private float bottomLimitY;
        private Vector3 velocity = Vector3.zero;

        private void Start()
        {
            if (target != null)
            {
                // Initialize the baseline to the camera's starting position
                bottomLimitY = transform.position.y;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // 1. Constantly force the absolute lowest point of the camera upwards over time
            bottomLimitY += autoScrollSpeed * Time.deltaTime;

            // 2. Where does the camera want to be to frame the player perfectly?
            float desiredY = target.position.y + yOffset;

            // 3. If the player speeds way ahead of the auto-scroll, pull the bottom limit up 
            // so they don't have a massive safety net if they mess up a jump.
            if (desiredY > bottomLimitY + maxFallDistance)
            {
                bottomLimitY = desiredY - maxFallDistance;
            }

            // 4. The camera can look up or down, but it CANNOT look below the rising limit
            float clampedY = Mathf.Max(desiredY, bottomLimitY);

            // 5. Smoothly move to the calculated position
            Vector3 targetPosition = new Vector3(transform.position.x, clampedY, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
        
        /// <summary>
        /// Call this when the player dies or the game resets to snap the camera back to the spawn.
        /// </summary>
        public void ResetCamera(Vector3 startPos)
        {
            transform.position = startPos;
            bottomLimitY = startPos.y;
            velocity = Vector3.zero;
        }
    }
}