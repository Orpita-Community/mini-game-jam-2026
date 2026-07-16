using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Orpaits.Collectibles;
using Orpaits.Player;

namespace Orpaits.UI
{
    /// <summary>
    /// Drives the in-game HUD: health bar, nostalgic-icon counter, ammo/disc
    /// counter, and shield indicator. Plus a hook for the Anti-Virus trade
    /// feedback animation (issue #22).
    ///
    /// All four data sources are decoupled — systems push values in via the
    /// Set* methods. The icon counter and health bar are auto-wired here
    /// (<see cref="IconCollectionManager.OnCountChanged"/> and
    /// <see cref="PlayerController.OnHealthChanged"/>). Ammo/discs and the
    /// shield stay at placeholders until issues #17 and #18 land their systems.
    ///
    /// Design reference: issue #13 (HUD), level-design-260712_2153.md (HUD layout),
    /// audio-design-260712_2153.md (Windows XP aesthetic).
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField]
        [Tooltip("Player whose health drives the bar. Found by tag at runtime if left empty.")]
        private PlayerController player;

        [SerializeField]
        [Tooltip("Tag used to find the player when the reference above is empty.")]
        private string playerTag = "Player";

        [Header("Health (top-left)")]
        [SerializeField]
        [Tooltip("Fill image whose width scales 0..1 with health fraction.")]
        private Image healthFill;

        [SerializeField]
        [Tooltip("Optional text like '87 / 100'. Leave null for fill-only.")]
        private TMP_Text healthText;

        [SerializeField]
        [Tooltip("Color at full health.")]
        private Color healthColorFull = new Color(0.42f, 0.85f, 0.32f);

        [SerializeField]
        [Tooltip("Color near death.")]
        private Color healthColorLow = new Color(0.85f, 0.18f, 0.18f);

        [Header("Icons (top-right)")]
        [SerializeField]
        [Tooltip("'x / 30' counter text.")]
        private TMP_Text iconCounterText;

        [SerializeField]
        [Tooltip("Optional small sprite shown next to the counter.")]
        private Image iconCounterSprite;

        [Header("Ammo / Discs (bottom-left)")]
        [SerializeField]
        [Tooltip("Disc count text.")]
        private TMP_Text ammoText;

        [Header("Shield (bottom-right)")]
        [SerializeField]
        [Tooltip("Shield root — disabled when no shield active.")]
        private GameObject shieldRoot;

        [SerializeField]
        [Tooltip("Radial/bar fill showing remaining shield time.")]
        private Image shieldTimerFill;

        [Header("Trade Feedback")]
        [SerializeField]
        [Tooltip("Optional animator/GameObject that plays the icons->gear animation.")]
        private GameObject tradeFeedbackPrefab;

        [SerializeField]
        [Tooltip("Where trade-feedback instances spawn (usually screen center).")]
        private Transform tradeFeedbackSpawn;

        /// <summary>Singleton accessor for systems that push updates.</summary>
        public static HUDManager Instance { get; private set; }

        private IconCollectionManager icons;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[HUDManager] Duplicate instance on '{name}' — destroying.", this);
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (icons != null) icons.OnCountChanged -= HandleIconCountChanged;
            if (player != null) player.OnHealthChanged -= SetHealth;

            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            // Icons (issue #16).
            icons = IconCollectionManager.Instance;
            if (icons != null)
            {
                SetIconCount(icons.Count, icons.TotalInLevel);
                icons.OnCountChanged += HandleIconCountChanged;
            }
            else
            {
                Debug.LogWarning("[HUDManager] No IconCollectionManager in scene; icon counter will be a placeholder.");
                SetIconCount(0, 0);
            }

            // Health — pushed by the player on damage, heal, and respawn.
            if (player == null)
            {
                var playerGO = GameObject.FindGameObjectWithTag(playerTag);
                if (playerGO != null) player = playerGO.GetComponent<PlayerController>();
            }

            if (player != null)
            {
                SetHealth(player.CurrentHealth, player.MaxHealth);
                player.OnHealthChanged += SetHealth;
            }
            else
            {
                Debug.LogWarning($"[HUDManager] No PlayerController tagged '{playerTag}'; health bar will be a placeholder.");
                SetHealth(1f, 1f);
            }

            // No system pushes these yet (#17 discs, #18 shield).
            SetAmmo(0);
            SetShield(false, 0f);
        }

        private void HandleIconCountChanged(int count)
        {
            SetIconCount(count, icons != null ? icons.TotalInLevel : 0);
        }

        // ---------------------------- API ----------------------------

        /// <summary>
        /// Update the health bar. <paramref name="current"/>/<paramref name="max"/>
        /// drives the fill and color lerp.
        /// </summary>
        public void SetHealth(float current, float max)
        {
            float frac = max <= 0f ? 0f : Mathf.Clamp01(current / max);
            if (healthFill != null)
            {
                healthFill.fillAmount = frac;
                healthFill.color = Color.Lerp(healthColorLow, healthColorFull, frac);
            }
            if (healthText != null)
                healthText.text = $"{Mathf.Max(0, current):0} / {max:0}";
        }

        /// <summary>
        /// Update the icon counter. Called automatically when
        /// <see cref="IconCollectionManager.OnCountChanged"/> fires.
        /// </summary>
        public void SetIconCount(int current, int total)
        {
            if (iconCounterText != null)
                iconCounterText.text = total > 0 ? $"{current} / {total}" : $"{current}";
        }

        /// <summary>
        /// Update the ammo/disc counter. Call from the throwable system (#17).
        /// </summary>
        public void SetAmmo(int count)
        {
            if (ammoText != null)
                ammoText.text = $"x {count}";
        }

        /// <summary>
        /// Show or hide the shield indicator with optional remaining-time
        /// fraction (0..1). Call from the shield system (#18).
        /// </summary>
        public void SetShield(bool active, float remainingFraction)
        {
            if (shieldRoot != null)
                shieldRoot.SetActive(active);
            if (active && shieldTimerFill != null)
                shieldTimerFill.fillAmount = Mathf.Clamp01(remainingFraction);
        }

        /// <summary>
        /// Spawn the trade-feedback animation. Called from the Anti-Virus
        /// NPC trade flow (#22). <paramref name="iconDelta"/> is the number of
        /// icons consumed; useful for the animation scale.
        /// </summary>
        public void PlayTradeFeedback(int iconDelta)
        {
            if (tradeFeedbackPrefab == null) return;
            var spawnAt = tradeFeedbackSpawn != null ? tradeFeedbackSpawn : transform;
            var fx = Instantiate(tradeFeedbackPrefab, spawnAt.position, Quaternion.identity, spawnAt);
            Debug.Log($"[HUDManager] Trade feedback spawned ({iconDelta} icons consumed).");
            // Animation self-destructs via its own Animator; we just host it.
        }
    }
}
