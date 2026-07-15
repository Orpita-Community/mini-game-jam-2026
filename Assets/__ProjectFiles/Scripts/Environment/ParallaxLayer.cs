using UnityEngine;

namespace Orpaits.Environment
{
    /// <summary>
    /// 2D parallax: layer moves at a fraction of camera movement, creating
    /// a depth illusion. Common use: clouds drift slowly with the camera,
    /// distant backgrounds lag behind foreground action.
    ///
    /// parallaxFactor semantics:
    ///   0.0 = layer doesn't move (fully static, scrolls past camera)
    ///   0.5 = layer moves at half camera speed (mid-depth parallax)
    ///   1.0 = layer moves 1:1 with camera (always same screen position)
    ///
    /// For a climber like Orpaits, clouds typically use 0.6–0.8 so they
    /// follow the climbing camera but appear farther away than platforms.
    /// </summary>
    [RequireComponent(typeof(Transform))]
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField]
        [Tooltip("Camera to follow. Defaults to Camera.main on Awake.")]
        private Camera mainCamera;

        [Header("Parallax")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("0 = static, 0.5 = half camera speed, 1 = 1:1 with camera.")]
        private float parallaxFactor = 0.7f;

        [SerializeField]
        [Tooltip("If true, X and Y use separate factors. If false, both use parallaxFactor.")]
        private bool perAxis = true;

        [SerializeField]
        [Tooltip("X-axis parallax (only used if perAxis is true).")]
        [Range(0f, 1f)]
        private float parallaxFactorX = 0.5f;

        [Header("Smoothing")]
        [SerializeField]
        [Tooltip("Smoothly damp toward the target position. Disable for 1:1 tracking.")]
        private bool smooth = true;

        [SerializeField]
        [Tooltip("SmoothDamp time (seconds). Lower = snappier.")]
        private float smoothTime = 0.2f;

        private Transform cam;
        private Vector3 layerStart;
        private Vector3 cameraStart;
        private Vector3 velocity;

        private void Awake()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null) cam = mainCamera.transform;
            layerStart = transform.position;
            cameraStart = cam != null ? cam.position : Vector3.zero;
        }

        private void LateUpdate()
        {
            if (cam == null) return;

            Vector3 cameraDelta = cam.position - cameraStart;

            float factorY = parallaxFactor;
            float factorX = perAxis ? parallaxFactorX : parallaxFactor;

            Vector3 target = layerStart + new Vector3(
                cameraDelta.x * factorX,
                cameraDelta.y * factorY,
                0f
            );
            target.z = layerStart.z; // never drift on Z

            if (smooth)
                transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, smoothTime);
            else
                transform.position = target;
        }

        /// <summary>
        /// Re-anchors the layer's "start" position to its current world
        /// position. Useful after manually moving the layer at runtime.
        /// </summary>
        public void Reanchor()
        {
            layerStart = transform.position;
            cameraStart = cam != null ? cam.position : Vector3.zero;
            velocity = Vector3.zero;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.6f); // sky blue
            // Visualize parallax factor in the inspector
            Gizmos.DrawWireCube(transform.position, new Vector3(2f, 2f, 0f));
        }
    }
}
