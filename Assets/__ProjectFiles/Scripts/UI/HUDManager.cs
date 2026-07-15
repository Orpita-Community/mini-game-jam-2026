using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Orpaits.Collectibles;

namespace Orpaits.UI
{
    /// <summary>
    /// Drives the in-game HUD: health bar, nostalgic-icon counter, ammo/disc
    /// counter, and shield indicator. Plus a hook for the Anti-Virus trade
    /// feedback animation (issue #22).
    ///
    /// All four data sources are decoupled — systems push values in via the
    /// Set* methods. As of this implementation only the icon counter is
    /// auto-wired (subscribes to <see cref="IconCollectionManager"/>); the
    /// other three remain at sensible placeholders until issues #2, #17, #18
    /// land their respective systems.
    ///
    /// Design reference: issue #13 (HUD), level-design-260712_2153.md (HUD layout),
    /// audio-design-260712_2153.md (Windows XP aesthetic).
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
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
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            // Wire to the icon collection if present (issue #16 already provides this).
            var icons = IconCollectionManager.Instance;
            if (icons != null)
            {
                SetIconCount(icons.Count, icons.TotalInLevel);
                icons.OnCountChanged += (count) => SetIconCount(count, icons.TotalInLevel);
            }
            else
            {
                Debug.LogWarning("[HUDManager] No IconCollectionManager in scene; icon counter will be a placeholder.");
                SetIconCount(0, 0);
            }

            // Initialize other slots to placeholder values until their systems exist.
            SetHealth(1f, 1f);
            SetAmmo(0);
            SetShield(false, 0f);
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
