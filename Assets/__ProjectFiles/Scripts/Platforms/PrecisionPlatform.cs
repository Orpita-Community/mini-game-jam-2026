using UnityEngine;

namespace Orpaits.Platforms
{
    /// <summary>
    /// A small or uniquely shaped platform that requires accurate jumps and careful timing.
    /// This is a marker component — its primary function is identification for level design,
    /// with configurable size categorization. These platforms create the peak platforming
    /// challenge in Zone 3 (Corrupted Core / High Directories).
    /// 
    /// Visual representation: thin window borders, narrow ledges, corner pieces.
    /// 
    /// Design reference: mechanics-260712_2137.md §7 (Precision Platform),
    /// game-loop-260712_2137.md (The High Directories)
    /// </summary>
    public class PrecisionPlatform : BasePlatform
    {
        public enum SizeCategory
        {
            /// <summary>One-player-width platform, requires accurate landing</summary>
            Small,

            /// <summary>Very thin ledge, pixel-perfect jumps required</summary>
            Narrow,

            /// <summary>Extremely small, corner-piece platform</summary>
            Tiny
        }

        [Header("Precision Platform")]
        [SerializeField]
        private SizeCategory sizeCategory = SizeCategory.Small;

        [SerializeField]
        [Tooltip("Difficulty rating (1-5) for this precision platform placement")]
        [Range(1, 5)]
        private int difficultyRating = 2;

        /// <summary>
        /// The size category of this precision platform.
        /// Affects visual representation and (optionally) collider size validation.
        /// </summary>
        public SizeCategory Category => sizeCategory;

        /// <summary>
        /// Difficulty rating for this specific platform placement (1-5).
        /// Higher values indicate tighter jump windows or more hazardous placement.
        /// </summary>
        public int DifficultyRating => difficultyRating;

        protected override void OnPlayerEnter(Collision2D collision)
        {
            // Precision platforms don't have special behaviors on contact.
            // Their challenge comes from the jump itself, not platform mechanics.
        }

        protected override void OnPlayerExit(Collision2D collision)
        {
            // No action needed on exit.
        }

        /// <summary>
        /// Validates that the platform collider dimensions match the selected size category.
        /// Logs a warning if there's a mismatch.
        /// </summary>
        private void OnValidate()
        {
            if (platformCollider == null)
                platformCollider = GetComponent<Collider2D>();

            if (platformCollider is BoxCollider2D box)
            {
                float width = box.size.x * transform.localScale.x;
                float height = box.size.y * transform.localScale.y;

                switch (sizeCategory)
                {
                    case SizeCategory.Tiny when width > 1.5f || height > 1.5f:
                        Debug.LogWarning($"[PrecisionPlatform] {name} marked as Tiny but size ({width:F1}x{height:F1}) seems large");
                        break;
                    case SizeCategory.Narrow when width > 2.5f && height < 0.5f:
                        // Narrow should be at least somewhat thin
                        break;
                }
            }
        }
    }
}
