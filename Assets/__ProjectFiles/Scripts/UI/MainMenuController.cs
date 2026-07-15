using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Orpaits.Core;

namespace Orpaits.UI
{
    /// <summary>
    /// Main Menu screen: "Start Game" / "Credits", plus a credits sub-panel.
    /// Lives in the MainMenu scene. Windows-XP styling is authored on the
    /// Canvas; this script only handles behaviour + SFX hooks.
    ///
    /// Design reference: issue #14 (Main Menu).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject creditsPanel;

        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button creditsBackButton;
        [SerializeField] private Button quitButton;

        [Header("Navigation")]
        [SerializeField] [Tooltip("Button auto-selected for keyboard/gamepad navigation.")]
        private GameObject firstSelected;

        [Header("SFX (optional)")]
        [SerializeField] private AudioSource uiAudio;
        [SerializeField] private AudioClip confirmSfx;

        private void Start()
        {
            Time.timeScale = 1f;
            ShowCredits(false);

            if (startButton != null) startButton.onClick.AddListener(OnStart);
            if (creditsButton != null) creditsButton.onClick.AddListener(() => { PlayConfirm(); ShowCredits(true); });
            if (creditsBackButton != null) creditsBackButton.onClick.AddListener(() => { PlayConfirm(); ShowCredits(false); });
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);

            SelectDefault();
        }

        private void OnStart()
        {
            PlayConfirm();
            if (GameManager.Instance != null) GameManager.Instance.StartGame();
            else SceneManager.LoadScene("SampleScene");
        }

        private void OnQuit()
        {
            PlayConfirm();
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
            else Application.Quit();
        }

        private void ShowCredits(bool show)
        {
            if (creditsPanel != null) creditsPanel.SetActive(show);
            if (mainPanel != null) mainPanel.SetActive(!show);
        }

        private void SelectDefault()
        {
            if (firstSelected == null || EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelected);
        }

        private void PlayConfirm()
        {
            if (uiAudio != null && confirmSfx != null) uiAudio.PlayOneShot(confirmSfx);
        }
    }
}
