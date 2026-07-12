using UnityEngine;

namespace Orpaits.Platforms
{
    public class PrecisionPlatform : BasePlatform
    {
        public enum SizeCategory { Small, Narrow, Tiny }

        [Header("Precision Platform")]
        [SerializeField] private SizeCategory sizeCategory = SizeCategory.Small;
        [SerializeField, Range(1, 5)] private int difficultyRating = 2;

        public SizeCategory Category => sizeCategory;
        public int Difficulty => difficultyRating;

        private void OnValidate()
        {
            if (platformCollider == null)
                platformCollider = GetComponent<Collider2D>();

            if (platformCollider is BoxCollider2D box)
            {
                var w = box.size.x * transform.localScale.x;
                var h = box.size.y * transform.localScale.y;
                if (sizeCategory == SizeCategory.Tiny && (w > 1.5f || h > 1.5f))
                    Debug.LogWarning($"[PrecisionPlatform] {name}: Tiny but size is {w:F1}x{h:F1}");
            }
        }
    }
}
