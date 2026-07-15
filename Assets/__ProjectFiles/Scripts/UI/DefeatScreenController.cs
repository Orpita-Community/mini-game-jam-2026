using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Orpaits.Core;
using Orpaits.Player;
using Orpaits.Environment;

namespace Orpaits.UI
{
    /// <summary>
    /// Handles the two lose conditions in a gameplay scene and drives the HUD
    /// health bar from the player.
    ///
    ///   Lose Condition A (Fatal Drop): the KillZone already respawns the
    ///   player at the latest checkpoint. We only flash a brief "SYSTEM CRASH"
    ///   overlay — fast auto-respawn, no blocking screen.
    ///
    ///   Lose Condition B (System Corruption / health = 0): show the full
    ///   "SYSTEM CRASH" Defeat screen with a Respawn button. Respawning revives
    ///   the player, restores health, and returns to the latest checkpoint.
    ///   Collected icons are always preserved (IconCollectionManager is untouched).
    ///
    /// Design reference: issue #24 (Lose Conditions A/B), issue #14 (Defeat Screen).
    /// </summary>
    public class DefeatScreenController : MonoBehaviour
    {
        [Header("References (auto-found if empty)")]
        [SerializeField] private PlayerController player;
        [SerializeField] private CheckpointManager checkpointManager;
        [SerializeField] private KillZone[] killZones;

        [Header("Defeat Screen (health = 0)")]
        [SerializeField] private GameObject defeatPanel;
        [SerializeField] private Button respawnButton;

        [Header("Fatal-Fall Flash (optional)")]
        [SerializeField] private CanvasGroup crashFlash;
        [SerializeField] private float flashDuration = 0.35f;

        [Header("HUD")]
        [SerializeField] [Tooltip("Push player health into HUDManager.SetHealth.")]
        private bool driveHudHealth = true;

        private void Awake()
        {
            if (player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) player = go.GetComponent<PlayerController>();
            }
            if (checkpointManager == null)
                checkpointManager = FindFirstObjectByType<CheckpointManager>();
            if (killZones == null || killZones.Length == 0)
                killZones = FindObjectsByType<KillZone>(FindObjectsSortMode.None);
        }

        private void OnEnable()
        {
            if (player != null)
            {
                player.OnDeath += HandleDeath;
                if (driveHudHealth) player.OnHealthChanged += HandleHealthChanged;
            }
            if (killZones != null)
                foreach (var kz in killZones)
                    if (kz != null) kz.OnPlayerKilled += HandleFatalFall;
        }

        private void OnDisable()
        {
            if (player != null)
            {
                player.OnDeath -= HandleDeath;
                player.OnHealthChanged -= HandleHealthChanged;
            }
            if (killZones != null)
                foreach (var kz in killZones)
                    if (kz != null) kz.OnPlayerKilled -= HandleFatalFall;
        }

        private void Start()
        {
            if (defeatPanel != null) defeatPanel.SetActive(false);
            if (crashFlash != null)
            {
                crashFlash.alpha = 0f;
                crashFlash.gameObject.SetActive(true);
            }
            if (respawnButton != null) respawnButton.onClick.AddListener(Respawn);

            // Prime the HUD with the starting health value.
            if (driveHudHealth && player != null)
                HandleHealthChanged(player.CurrentHealth, player.MaxHealth);
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (HUDManager.Instance != null)
                HUDManager.Instance.SetHealth(current, max);
        }

        private void HandleDeath()
        {
            // Lose Condition B: freeze and show the Defeat screen.
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Defeat);
            Time.timeScale = 0f;
            if (defeatPanel != null) defeatPanel.SetActive(true);
        }

        private void HandleFatalFall()
        {
            // Lose Condition A: KillZone already respawns; just flash the screen.
            if (crashFlash != null && isActiveAndEnabled)
                StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            crashFlash.alpha = 1f;
            float t = 0f;
            while (t < flashDuration)
            {
                t += Time.unscaledDeltaTime;
                crashFlash.alpha = Mathf.Lerp(1f, 0f, t / flashDuration);
                yield return null;
            }
            crashFlash.alpha = 0f;
        }

        private void Respawn()
        {
            if (defeatPanel != null) defeatPanel.SetActive(false);
            Time.timeScale = 1f;
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Playing);

            // Revives the player, restores health, and moves to the latest checkpoint.
            if (checkpointManager != null)
                checkpointManager.RespawnAtCheckpoint();
        }
    }
}
