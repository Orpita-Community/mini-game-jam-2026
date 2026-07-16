using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Orpaits.Enemies;

namespace Orpaits.UI
{
    /// <summary>
    /// Dedicated health bar for the Boss Virus fight.
    ///
    /// Subscribes to BossVirus health and phase events. The bar is hidden
    /// until the fight starts (StartBossFight) and stays visible through
    /// all 3 phases, changing color per phase:
    ///   Phase 1 (The Spam)      — blue
    ///   Phase 2 (The Deletion)  — orange
    ///   Phase 3 (Glitch Wave)   — red, with a glitch flicker effect
    ///
    /// Design reference: level-design-260712_2153.md (Boss Fight — Health Bar),
    /// issue #13 (HUD).
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        [Header("Boss Reference (auto-found if empty)")]
        [SerializeField]
        private BossVirus boss;

        [Header("UI Elements")]
        [SerializeField]
        [Tooltip("Root GameObject — shown/hidden when fight starts/ends.")]
        private GameObject barRoot;

        [SerializeField]
        [Tooltip("Fill image whose fillAmount scales 0..1 with boss HP.")]
        private Image fillImage;

        [SerializeField]
        [Tooltip("Optional boss name text (e.g. 'THE VIRUS').")]
        private TMP_Text nameText;

        [SerializeField]
        [Tooltip("Optional phase label (e.g. 'PHASE 1 — THE SPAM').")]
        private TMP_Text phaseText;

        [Header("Phase Colors")]
        [SerializeField]
        private Color phase1Color = new Color(0.25f, 0.55f, 1f);

        [SerializeField]
        private Color phase2Color = new Color(1f, 0.55f, 0.1f);

        [SerializeField]
        private Color phase3Color = new Color(0.9f, 0.15f, 0.15f);

        [Header("Phase 3 Glitch")]
        [SerializeField]
        [Tooltip("How fast the bar flickers during Phase 3.")]
        private float glitchFlickerSpeed = 12f;

        [SerializeField]
        [Tooltip("How far the bar jitters during Phase 3 (pixels).")]
        private float glitchJitterAmount = 3f;

        private RectTransform barRect;
        private Vector2 barAnchoredPos;
        private bool glitching;

        private void Awake()
        {
            if (barRoot != null)
                barRect = barRoot.GetComponent<RectTransform>();

            if (barRect != null)
                barAnchoredPos = barRect.anchoredPosition;
        }

        private void OnEnable()
        {
            if (boss == null) boss = FindFirstObjectByType<BossVirus>();
            if (boss == null)
            {
                Debug.LogWarning("[BossHealthBar] No BossVirus found in scene.", this);
                return;
            }

            boss.OnHealthChanged += HandleHealthChanged;
            boss.OnPhaseChanged += HandlePhaseChanged;
            boss.OnBossDefeated += HandleBossDefeated;
        }

        private void OnDisable()
        {
            if (boss == null) return;

            boss.OnHealthChanged -= HandleHealthChanged;
            boss.OnPhaseChanged -= HandlePhaseChanged;
            boss.OnBossDefeated -= HandleBossDefeated;
        }

        private void Start()
        {
            // Hidden until the fight starts
            if (barRoot != null) barRoot.SetActive(false);

            if (nameText != null && boss != null)
            {
                // Use reflection-safe default since bossName is private
                nameText.text = "THE VIRUS";
            }

            UpdatePhaseDisplay(BossVirus.BossPhase.Phase1);
        }

        private void Update()
        {
            if (!glitching || barRect == null) return;

            // Glitch flicker: toggle between phase3 color and a darker shade
            float flicker = Mathf.Repeat(Time.time * glitchFlickerSpeed, 1f);
            if (fillImage != null)
            {
                float lerp = Mathf.PingPong(flicker, 0.5f) * 2f;
                fillImage.color = Color.Lerp(phase3Color, phase3Color * 0.4f, lerp);
            }

            // Position jitter
            float jx = (Mathf.PerlinNoise(Time.time * 20f, 0f) - 0.5f) * glitchJitterAmount;
            float jy = (Mathf.PerlinNoise(0f, Time.time * 20f) - 0.5f) * glitchJitterAmount;
            barRect.anchoredPosition = barAnchoredPos + new Vector2(jx, jy);
        }

        // ── Event Handlers ──────────────────────────────────────────────

        /// <summary>
        /// Called by StartBossFight indirectly — the first OnHealthChanged
        /// or OnPhaseChanged event means the fight is active, so we reveal the bar.
        /// </summary>
        private void HandleHealthChanged(float current, float max)
        {
            if (barRoot != null && !barRoot.activeSelf)
                barRoot.SetActive(true);

            if (fillImage != null)
                fillImage.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        }

        private void HandlePhaseChanged(BossVirus.BossPhase newPhase)
        {
            if (barRoot != null && !barRoot.activeSelf)
                barRoot.SetActive(true);

            UpdatePhaseDisplay(newPhase);
        }

        private void HandleBossDefeated()
        {
            glitching = false;

            if (fillImage != null)
                fillImage.fillAmount = 0f;

            // Hide after a short delay so the player sees it empty
            // (VictorySequenceController handles the full fade)
            if (barRect != null)
                barRect.anchoredPosition = barAnchoredPos;

            if (phaseText != null)
                phaseText.text = "DEFEATED";
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private void UpdatePhaseDisplay(BossVirus.BossPhase phase)
        {
            glitching = false;

            // Restore bar position when leaving glitch mode
            if (barRect != null)
                barRect.anchoredPosition = barAnchoredPos;

            switch (phase)
            {
                case BossVirus.BossPhase.Phase1:
                    if (fillImage != null) fillImage.color = phase1Color;
                    if (phaseText != null) phaseText.text = "PHASE 1 — THE SPAM";
                    break;

                case BossVirus.BossPhase.Phase2:
                    if (fillImage != null) fillImage.color = phase2Color;
                    if (phaseText != null) phaseText.text = "PHASE 2 — THE DELETION";
                    break;

                case BossVirus.BossPhase.Phase3:
                    if (fillImage != null) fillImage.color = phase3Color;
                    if (phaseText != null) phaseText.text = "PHASE 3 — GLITCH WAVE";
                    glitching = true;
                    break;
            }
        }
    }
}
