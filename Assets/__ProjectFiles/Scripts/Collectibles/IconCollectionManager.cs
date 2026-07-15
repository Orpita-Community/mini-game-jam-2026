using System.Collections.Generic;
using UnityEngine;

namespace Orpaits.Collectibles
{
    /// <summary>
    /// Scene-level inventory tracker for collected nostalgic icons.
    ///
    /// This is a single instance per scene. Subscribes to (or is polled by)
    /// the HUD for the live counter, and exposes a <see cref="SpendIcons"/>
    /// method used by the Anti-Virus NPC trade (issue #22).
    ///
    /// Persistence: collected icons survive death/respawn naturally because
    /// respawn (see <see cref="Orpaits.Environment.CheckpointManager"/>) only
    /// moves the player — this GameObject and its state are untouched. For a
    /// full game restart, call <see cref="ResetForNewGame"/>.
    ///
    /// Design reference: mechanics-260712_2137.md (Collectible System §2),
    /// level-design-260712_2153.md (30 total icons / 20 needed for trade).
    /// </summary>
    public class IconCollectionManager : MonoBehaviour
    {
        [Header("Trade Thresholds (per issue #22)")]
        [SerializeField]
        [Tooltip("Icons required to unlock the standard trade (Shield + Discs).")]
        private int standardTradeCost = 20;

        [SerializeField]
        [Tooltip("Icons required to unlock the bonus trade. Always >= standardTradeCost.")]
        private int bonusTradeCost = 25;

        [Header("Total in level (per issue #16)")]
        [SerializeField]
        [Tooltip("Expected total icons placed in the level. Used for the HUD x/30 counter.")]
        private int totalInLevel = 30;

        private readonly List<IconData> collected = new();

        /// <summary>
        /// Singleton accessor. Resolved lazily on first <see cref="AddIcon"/> call;
        /// also cached here for low-latency HUD / NPC polling.
        /// </summary>
        public static IconCollectionManager Instance { get; private set; }

        /// <summary>Number of icons currently in the inventory.</summary>
        public int Count => collected.Count;

        /// <summary>Total icons the HUD should display as the denominator.</summary>
        public int TotalInLevel => totalInLevel;

        /// <summary>Read-only view of collected icons, in collection order.</summary>
        public IReadOnlyList<IconData> Collected => collected;

        /// <summary>Fires with the new count whenever an icon is added or spent.</summary>
        public event System.Action<int> OnCountChanged;

        /// <summary>Fires with the icon data whenever a single icon is added.</summary>
        public event System.Action<IconData> OnIconAdded;

        /// <summary>Fires with the count spent whenever icons are consumed by trade.</summary>
        public event System.Action<int> OnIconsSpent;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    $"[IconCollectionManager] Multiple instances found. Destroying duplicate on '{name}'.",
                    this);
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Adds an icon to the inventory. Called by <see cref="IconPickup.Collect"/>.
        /// Safe to call multiple times for the same icon if you know what you're doing;
        /// each call increments the count.
        /// </summary>
        public void AddIcon(IconData icon)
        {
            if (icon == null) return;
            collected.Add(icon);
            OnIconAdded?.Invoke(icon);
            OnCountChanged?.Invoke(collected.Count);
        }

        /// <summary>
        /// Spends up to <paramref name="count"/> icons on a trade. Returns false
        /// if the inventory has fewer than <paramref name="count"/> icons.
        /// Removes from the front of the list (oldest collected first).
        /// </summary>
        public bool SpendIcons(int count)
        {
            if (count <= 0) return true;
            if (collected.Count < count) return false;

            collected.RemoveRange(0, count);
            OnIconsSpent?.Invoke(count);
            OnCountChanged?.Invoke(collected.Count);
            return true;
        }

        /// <summary>
        /// Has the player collected enough icons for the standard trade
        /// (Shield + Discs)?
        /// </summary>
        public bool MeetsStandardTrade => collected.Count >= standardTradeCost;

        /// <summary>
        /// Has the player collected enough icons for the bonus trade?
        /// </summary>
        public bool MeetsBonusTrade => collected.Count >= bonusTradeCost;

        /// <summary>
        /// Clears the inventory. Intended for a full game restart — NOT for
        /// death/respawn (icons persist across respawn by design).
        /// </summary>
        public void ResetForNewGame()
        {
            int prev = collected.Count;
            collected.Clear();
            if (prev != 0)
                OnCountChanged?.Invoke(0);
            Debug.Log("[IconCollectionManager] Inventory reset for new game.");
        }

        /// <summary>Convenience: format like "12/30" for HUDs.</summary>
        public string FormatProgress() => $"{collected.Count}/{totalInLevel}";
    }
}
