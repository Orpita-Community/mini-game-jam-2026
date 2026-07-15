using UnityEngine;

namespace Orpaits.Environment
{
    /// <summary>
    /// Wraps a cloud sprite horizontally so it cycles when it drifts off-screen
    /// via parallax. Attach to each cloud child inside a ParallaxLayer container.
    ///
    /// When the cloud's local X exceeds <c>wrapHalfWidth</c> in either direction,
    /// it wraps to the opposite side, creating an endless horizontal cycle.
    ///
    /// Uses [DefaultExecutionOrder(100)] to run after ParallaxLayer (order 0)
    /// so the parent container has already moved this frame.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class CloudWrap : MonoBehaviour
    {
        [Header("Wrapping")]
        [SerializeField]
        [Tooltip("Cloud wraps when its local X exceeds this distance from center.")]
        private float wrapHalfWidth = 20f;

        [SerializeField]
        [Tooltip("Small random Y offset applied on wrap for visual variety.")]
        private float yJitter = 0.5f;

        private void LateUpdate()
        {
            Vector3 localPos = transform.localPosition;
            float half = wrapHalfWidth;
            float full = half * 2f;

            bool wrapped = false;

            if (localPos.x < -half)
            {
                localPos.x += full;
                wrapped = true;
            }
            else if (localPos.x > half)
            {
                localPos.x -= full;
                wrapped = true;
            }

            if (wrapped && yJitter > 0f)
            {
                localPos.y += Random.Range(-yJitter, yJitter);
            }

            transform.localPosition = localPos;
        }
    }
}
