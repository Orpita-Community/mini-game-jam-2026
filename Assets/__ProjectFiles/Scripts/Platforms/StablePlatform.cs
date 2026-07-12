using UnityEngine;

namespace Orpaits.Platforms
{
    /// <summary>
    /// A standard, safe platform with no special behavior.
    /// Remains stationary and provides a stable surface for the player.
    /// Used as safe zones for planning jumps and recovering from difficult sections.
    /// Most common in Zone 1 (Boot Sequence), decreasing in frequency as difficulty rises.
    /// </summary>
    public class StablePlatform : BasePlatform
    {
        [Header("Stable Platform")]
        [SerializeField]
        [Tooltip("Optional: visual variant identifier for level design")]
        private string variant = "Default";

        protected override void OnPlayerEnter(Collision2D collision)
        {
            // Stable platform has no special behavior on player contact.
            // This hook is available for future SFX (e.g., a solid "thud" landing sound).
        }

        protected override void OnPlayerExit(Collision2D collision)
        {
            // No action needed on exit.
        }

        public override void ResetPlatform()
        {
            // Stable platforms don't need state reset — they're always static.
            base.ResetPlatform();
        }
    }
}
