using UnityEngine;

namespace Orpaits.Collectibles
{
    /// <summary>
    /// Defines a single nostalgic icon variant (Mario, Sonic, Pac-Man, ...).
    /// A ScriptableObject asset is created per variant; IconPickup references
    /// one of these to know what it represents.
    ///
    /// Design reference: mechanics-260712_2137.md (Collectible System §2),
    /// level-design-260712_2153.md (Nostalgic/retro game files list).
    /// </summary>
    [CreateAssetMenu(fileName = "IconData", menuName = "Orpaits/Icon Data", order = 10)]
    public class IconData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        [Tooltip("Human-readable name shown in HUD/dialog, e.g. 'Plumber Bro'.")]
        private string displayName = "New Icon";

        [SerializeField]
        [Tooltip("Optional short flavor / origin reference. Leave empty if none.")]
        private string description;

        [Header("Visuals")]
        [SerializeField]
        [Tooltip("Sprite used for both the in-world pickup and the HUD icon.")]
        private Sprite sprite;

        [SerializeField]
        [Tooltip("Tint applied to the sprite. White = no tint.")]
        private Color tint = Color.white;

        [Header("Gameplay")]
        [SerializeField]
        [Tooltip("Rarity tier. Drives placement strategy and pickup VFX intensity.")]
        private Rarity rarity = Rarity.Common;

        /// <summary>Display name (HUD / dialog).</summary>
        public string DisplayName => displayName;

        /// <summary>Optional flavor text.</summary>
        public string Description => description;

        /// <summary>Sprite shown in-world and in the HUD.</summary>
        public Sprite Sprite => sprite;

        /// <summary>Tint applied to the sprite.</summary>
        public Color Tint => tint;

        /// <summary>Rarity tier.</summary>
        public Rarity ItemRarity => rarity;

        private void Reset()
        {
            // Sensible default for newly-created assets
            displayName = name;
        }
    }

    /// <summary>
    /// Rarity tier for an icon. Drives placement tier and VFX feel.
    /// </summary>
    public enum Rarity
    {
        /// <summary>Easy tier - on the main path, hard to miss.</summary>
        Common,

        /// <summary>Risky tier - over corrupted/shaking platforms.</summary>
        Rare,

        /// <summary>Hidden tier - optional challenge, off the beaten path.</summary>
        Legendary
    }
}
