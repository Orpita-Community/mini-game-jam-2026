using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Orpaits.Core;

namespace Orpaits.UI
{
    /// <summary>
    /// Pause menu for gameplay scenes. Esc toggles a darkening overlay with
    /// Resume / Quit-to-Menu, freezing gameplay via <see cref="GameManager.SetPaused"/>.
    /// Only pauses from the Playing state, so it can't interrupt Victory/Defeat.
    ///
    /// Design reference: issue #14 (Pause Menu).
    /// </summary>
    public class PauseController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button quitButton;
        [SerializeField] [Tooltip("Button auto-selected when the menu opens.")]
        private GameObject firstSelected;

        [Header("SFX (optional)")]
        [SerializeField] private AudioSource uiAudio;
        [SerializeField] private AudioClip confirmSfx;

        private void Start()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (quitButton != null) quitButton.onClick.AddListener(QuitToMenu);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
                Toggle();
        }

        private void Toggle()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (gm.State == GameState.Playing) Pause();
            else if (gm.State == GameState.Paused) Resume();
        }

        private void Pause()
        {
            GameManager.Instance.SetPaused(true);
            if (pausePanel != null) pausePanel.SetActive(true);

            if (firstSelected != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(firstSelected);
            }
        }

        private void Resume()
        {
            PlayConfirm();
            if (pausePanel != null) pausePanel.SetActive(false);
            GameManager.Instance.SetPaused(false);
        }

        private void QuitToMenu()
        {
            PlayConfirm();
            if (pausePanel != null) pausePanel.SetActive(false);
            Time.timeScale = 1f;
            GameManager.Instance.LoadMainMenu();
        }

        private void PlayConfirm()
        {
            if (uiAudio != null && confirmSfx != null) uiAudio.PlayOneShot(confirmSfx);
        }
    }
}
