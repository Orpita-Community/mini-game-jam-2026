using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Orpaits.Collectibles;
using Orpaits.Core;
using Orpaits.UI;

namespace Orpaits.NPC
{
    /// <summary>
    /// The Anti-Virus NPC at the top of Zone 3. Trades collected nostalgic
    /// icons for boss-fight gear (Shield + Data Discs), then loads the Boss
    /// Arena scene. Persists trade state via PlayerPrefs so the player cannot
    /// re-trade after a successful exchange (even across scene reloads).
    ///
    /// Trade tiers (per issue #22):
    ///   &lt; 20 icons → error message, trade blocked, boss door stays closed
    ///   20-24 icons → standard trade (Shield + Data Discs)
    ///   25+ icons   → standard + bonus gear
    ///
    /// Design reference: mechanics-260712_2137.md (Trading System §3),
    /// level-design-260712_2153.md (The Anti-Virus Trade),
    /// game-loop-260712_2137.md (Transaction Phase).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class AntiVirusNPC : MonoBehaviour, IPowerTradeAudioSource
    {
        [Header("Identity")]
        [SerializeField]
        [Tooltip("Name shown in the dialog header.")]
        private string npcName = "ANTI-Virus";

        [Header("Trade Thresholds")]
        [SerializeField]
        [Tooltip("Icons required for the standard trade.")]
        private int standardTradeCost = 20;

        [SerializeField]
        [Tooltip("Icons required for the bonus trade (always >= standardTradeCost).")]
        private int bonusTradeCost = 25;

        [Header("Dialog")]
        [SerializeField]
        [Tooltip("Dialog UI root (Canvas). Auto-shown when player enters range.")]
        private GameObject dialogRoot;

        [SerializeField]
        private TextMeshProUGUI nameText;

        [SerializeField]
        private TextMeshProUGUI messageText;

        [SerializeField]
        [Tooltip("Trade button — disabled when player can't afford the trade.")]
        private Button tradeButton;

        [SerializeField]
        [Tooltip("Cancel button — always enabled.")]
        private Button cancelButton;

        [Header("Post-Trade")]
        [SerializeField]
        [Tooltip("Name of the Boss Arena scene (must be in Build Settings).")]
        private string bossArenaSceneName = "BossArena";

        [SerializeField]
        [Tooltip("Optional boss door / blockade that opens (deactivates) after trade.")]
        private GameObject bossDoor;

        [SerializeField]
        [Tooltip("Optional sprite shown after the NPC becomes inactive (post-trade).")]
        private Sprite inactiveSprite;

        [Header("Persistence")]
        [SerializeField]
        [Tooltip("PlayerPrefs key used to remember that this NPC has traded.")]
        private string tradePlayerPrefsKey = "Antivirus_Trade_Completed";

        [Header("Animation")]
        [SerializeField]
        [Tooltip("Animator driving Idle/Talking states. Bool parameter 'IsTalking' is set automatically.")]
        private Animator animator;

        [Header("Audio")]
        [SerializeField]
        private AudioClip powerTradeSfx;

        /// <summary>Animator bool parameter name used to switch to Talking state.</summary>
        public const string IsTalkingParam = "IsTalking";

        public AudioClip PowerTradeSfx => powerTradeSfx;

        public event System.Action OnTradeCompleted;

        // ───────────────────────────── State ─────────────────────────────

        /// <summary>Has this NPC already completed a trade (current or prior session)?</summary>
        public bool HasTraded { get; private set; }

        /// <summary>Is the player currently in interaction range?</summary>
        public bool PlayerInRange { get; private set; }

        private SpriteRenderer spriteRenderer;

        // ─────────────────────────── Lifecycle ───────────────────────────

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (animator == null) animator = GetComponent<Animator>();
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;

            // Restore trade state from PlayerPrefs
            HasTraded = PlayerPrefs.GetInt(tradePlayerPrefsKey, 0) == 1;

            if (HasTraded)
                BecomeInactive();
            else if (dialogRoot != null)
                dialogRoot.SetActive(false); // hidden until player approaches
        }

        private void OnEnable()
        {
            if (tradeButton != null) tradeButton.onClick.AddListener(OnTradeClicked);
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);
        }

        private void OnDisable()
        {
            if (tradeButton != null) tradeButton.onClick.RemoveListener(OnTradeClicked);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(OnCancelClicked);
        }

        // ───────────────────────── Proximity ─────────────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (HasTraded) return;
            if (!other.CompareTag("Player")) return;
            PlayerInRange = true;
            ShowDialog();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            PlayerInRange = false;
            HideDialog();
        }

        // ─────────────────────────── Dialog ──────────────────────────────

        private void ShowDialog()
        {
            if (dialogRoot == null)
            {
                Debug.LogError("[AntiVirusNPC] No dialogRoot assigned!", this);
                return;
            }

            dialogRoot.SetActive(true);
            if (nameText != null) nameText.text = npcName;
            SetTalking(true);

            var icons = IconCollectionManager.Instance;
            int count = icons != null ? icons.Count : 0;

            // Branch on icon count
            if (count < standardTradeCost)
            {
                if (messageText != null)
                    messageText.text = $"You have {count} nostalgic icons. I need at least {standardTradeCost} to forge you boss-fighting gear. Collect more and return!";
                if (tradeButton != null) tradeButton.interactable = false;
            }
            else if (count < bonusTradeCost)
            {
                if (messageText != null)
                    messageText.text = $"You have {count} icons. I can trade {standardTradeCost} for a Shield + Data Discs. Deal?";
                if (tradeButton != null) tradeButton.interactable = true;
            }
            else
            {
                if (messageText != null)
                    messageText.text = $"You have {count} icons! Trading {standardTradeCost} for Shield + Data Discs, plus BONUS gear. Deal?";
                if (tradeButton != null) tradeButton.interactable = true;
            }
        }

        private void HideDialog()
        {
            if (dialogRoot != null) dialogRoot.SetActive(false);
            SetTalking(false);
        }

        /// <summary>Toggles the Animator between Idle and Talking states.</summary>
        private void SetTalking(bool talking)
        {
            if (animator != null)
                animator.SetBool(IsTalkingParam, talking);
        }

        // ─────────────────────────── Trade ───────────────────────────────

        /// <summary>Trade button handler. Validates + executes the trade.</summary>
        public void OnTradeClicked()
        {
            if (HasTraded) return;
            var icons = IconCollectionManager.Instance;
            if (icons == null)
            {
                Debug.LogError("[AntiVirusNPC] No IconCollectionManager in scene!", this);
                return;
            }
            if (icons.Count < standardTradeCost)
            {
                Debug.LogWarning("[AntiVirusNPC] Player cannot afford trade (shouldn't happen — button disabled).");
                return;
            }

            bool spent = icons.SpendIcons(standardTradeCost);
            if (!spent)
            {
                Debug.LogError("[AntiVirusNPC] SpendIcons failed despite check passing.", this);
                return;
            }

            // Award gear (stubs — wire to Shield/Disc systems when #17/#18 land)
            AwardGear(icons.Count >= bonusTradeCost - standardTradeCost);

            // HUD feedback
            if (HUDManager.Instance != null)
                HUDManager.Instance.PlayTradeFeedback(standardTradeCost);

            // Persist
            HasTraded = true;
            PlayerPrefs.SetInt(tradePlayerPrefsKey, 1);
            PlayerPrefs.Save();

            HideDialog();
            BecomeInactive();

            OnTradeCompleted?.Invoke();

            // Open the boss door (if assigned) and load the arena scene
            if (bossDoor != null) bossDoor.SetActive(false);
            LoadBossArena();
        }

        /// <summary>Cancel button handler. Just hides the dialog.</summary>
        public void OnCancelClicked()
        {
            HideDialog();
        }

        // ─────────────────────── Reward / Scene ──────────────────────────

        private void AwardGear(bool bonus)
        {
            // TODO (#17 Data Disc / #18 Shield): when those systems land, give the player
            //   1x Shield + N Data Discs (+ bonus equivalents if bonus==true).
            //   For now we just log; the trade still completes and the arena loads.
            Debug.Log($"[AntiVirusNPC] Awarded gear. Bonus={bonus}. (Wire to Shield/Disc systems in #17/#18.)");
        }

        private void LoadBossArena()
        {
            if (string.IsNullOrEmpty(bossArenaSceneName))
            {
                Debug.LogError("[AntiVirusNPC] bossArenaSceneName is empty — cannot load scene.", this);
                return;
            }
            Debug.Log($"[AntiVirusNPC] Loading scene: {bossArenaSceneName}");
            SceneManager.LoadScene(bossArenaSceneName);
        }

        private void BecomeInactive()
        {
            // Visually mark NPC as spent
            if (inactiveSprite != null && spriteRenderer != null)
                spriteRenderer.sprite = inactiveSprite;
        }

        // ─────────────────────────── Debug ───────────────────────────────

        /// <summary>Editor-only: clears the PlayerPrefs flag so the NPC can trade again.</summary>
        public void DebugResetTrade()
        {
            PlayerPrefs.DeleteKey(tradePlayerPrefsKey);
            PlayerPrefs.Save();
            HasTraded = false;
            Debug.Log("[AntiVirusNPC] Trade state reset.");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.85f, 0.85f, 0.5f); // cyan
            var col = GetComponent<Collider2D>();
            if (col != null)
                Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }
    }
}
