using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Orpaits.Core;
using Orpaits.Enemies;

namespace Orpaits.UI
{
    /// <summary>
    /// Drives the win condition + ending sequence in the Boss Arena.
    ///
    /// When <see cref="BossVirus.OnBossDefeated"/> fires:
    ///   1. State -> Victory, freeze all enemies (Virus Purge plays on the boss).
    ///   2. Hold for the purge duration, then fade to black.
    ///   3. Freeze time and show the Victory screen ("Play Again" / "Credits")
    ///      with an optional 8-bit fanfare.
    ///
    /// Design reference: issue #24 (Win Condition &amp; Victory Sequence),
    /// issue #14 (Victory Screen).
    /// </summary>
    public class VictorySequenceController : MonoBehaviour
    {
        [Header("References (auto-found if empty)")]
        [SerializeField] private BossVirus boss;

        [Header("Sequence Timing")]
        [SerializeField] [Tooltip("How long the Virus Purge plays before fading.")]
        private float purgeDuration = 2f;
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private float fadeDuration = 1.2f;

        [Header("Victory Screen")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button creditsBackButton;

        [Header("Audio (optional)")]
        [SerializeField] private AudioSource fanfareAudio;
        [SerializeField] private AudioClip fanfareClip;

        private bool triggered;

        private void Awake()
        {
            if (boss == null) boss = FindFirstObjectByType<BossVirus>();
        }

        private void OnEnable()
        {
            if (boss != null) boss.OnBossDefeated += HandleBossDefeated;
        }

        private void OnDisable()
        {
            if (boss != null) boss.OnBossDefeated -= HandleBossDefeated;
        }

        private void Start()
        {
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (fadeOverlay != null)
            {
                fadeOverlay.alpha = 0f;
                fadeOverlay.gameObject.SetActive(true);
            }
            if (playAgainButton != null) playAgainButton.onClick.AddListener(PlayAgain);
            if (creditsButton != null) creditsButton.onClick.AddListener(() => ShowCredits(true));
            if (creditsBackButton != null) creditsBackButton.onClick.AddListener(() => ShowCredits(false));
        }

        private void HandleBossDefeated()
        {
            if (triggered) return;
            triggered = true;
            StartCoroutine(VictoryRoutine());
        }

        private IEnumerator VictoryRoutine()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Victory);

            FreezeEnemies();

            // Virus Purge window (boss plays its glimmer/explode animation).
            yield return new WaitForSecondsRealtime(purgeDuration);

            // Fade to black (unscaled so it survives the time freeze below).
            if (fadeOverlay != null)
            {
                float t = 0f;
                while (t < fadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    fadeOverlay.alpha = Mathf.Clamp01(t / fadeDuration);
                    yield return null;
                }
                fadeOverlay.alpha = 1f;
            }

            Time.timeScale = 0f;
            if (victoryPanel != null) victoryPanel.SetActive(true);
            if (fanfareAudio != null && fanfareClip != null) fanfareAudio.PlayOneShot(fanfareClip);
        }

        private void FreezeEnemies()
        {
            var enemies = FindObjectsByType<BaseEnemy>(FindObjectsSortMode.None);
            foreach (var e in enemies)
                if (e != null) e.enabled = false;
        }

        private void ShowCredits(bool show)
        {
            if (creditsPanel != null) creditsPanel.SetActive(show);
            if (victoryPanel != null) victoryPanel.SetActive(!show);
        }

        private void PlayAgain()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null) GameManager.Instance.StartGame();
            else SceneManager.LoadScene("SampleScene");
        }
    }
}
