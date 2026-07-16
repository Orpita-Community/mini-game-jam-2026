using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
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
        [SerializeField] private GameObject settingsPanel;

        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button creditsBackButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button settingsBackButton;
        [SerializeField] private Button musicMuteButton;
        [SerializeField] private Button sfxMuteButton;
        [SerializeField] private Button quitButton;

        [Header("Sliders")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;

        [Header("Navigation")]
        [SerializeField] [Tooltip("Button auto-selected for keyboard/gamepad navigation.")]
        private GameObject firstSelected;

        [Header("SFX (optional)")]
        [SerializeField] private AudioSource uiAudio;
        [SerializeField] private AudioClip confirmSfx;

        [Header("Audio UI Tint")]
        [SerializeField] private Color enabledTint = new Color(1f, 1f, 1f, 1f);

        [SerializeField] private Color mutedTint = new Color(0.65f, 0.65f, 0.65f, 100f / 255f);

        private void Start()
        {
            Time.timeScale = 1f;
            ShowCredits(false);
            ShowSettings(false);

            if (startButton != null) startButton.onClick.AddListener(OnStart);
            if (creditsButton != null) creditsButton.onClick.AddListener(() => { PlayConfirm(); ShowCredits(true); });
            if (creditsBackButton != null) creditsBackButton.onClick.AddListener(() => { PlayConfirm(); ShowCredits(false); });
            if (settingsButton != null) settingsButton.onClick.AddListener(() => { PlayConfirm(); ShowSettings(true); });
            if (settingsBackButton != null) settingsBackButton.onClick.AddListener(() => { PlayConfirm(); ShowSettings(false); });
            if (musicMuteButton != null) musicMuteButton.onClick.AddListener(OnMusicMutePressed);
            if (sfxMuteButton != null) sfxMuteButton.onClick.AddListener(OnSfxMutePressed);
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);

            SyncSettingsUiFromAudio();

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

        private void ShowSettings(bool show)
        {
            if (settingsPanel != null) settingsPanel.SetActive(show);
            if (mainPanel != null) mainPanel.SetActive(!show);

            if (show)
                SyncSettingsUiFromAudio();
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

        private void OnMusicMutePressed()
        {
            PlayConfirm();
            AudioSettingsChannel.ToggleMusicMuted();

            SyncSettingsUiFromAudio();
        }

        private void OnSfxMutePressed()
        {
            PlayConfirm();
            AudioSettingsChannel.ToggleSfxMuted();

            SyncSettingsUiFromAudio();
        }

        private void OnMusicSliderChanged(float value)
        {
            AudioSettingsChannel.SetMusicVolume(value);
        }

        private void OnSfxSliderChanged(float value)
        {
            AudioSettingsChannel.SetSfxVolume(value);
        }

        private void SyncSettingsUiFromAudio()
        {
            if (musicSlider != null)
                musicSlider.SetValueWithoutNotify(AudioSettingsChannel.MusicVolume);

            if (sfxSlider != null)
                sfxSlider.SetValueWithoutNotify(AudioSettingsChannel.SfxVolume);

            UpdateMuteButtonLabel(musicMuteButton, AudioSettingsChannel.IsMusicMuted, "Music: ON", "Music: OFF");
            UpdateMuteButtonLabel(sfxMuteButton, AudioSettingsChannel.IsSfxMuted, "SFX: ON", "SFX: OFF");

            SetSelectableTint(musicMuteButton, musicSlider, AudioSettingsChannel.IsMusicMuted);
            SetSelectableTint(sfxMuteButton, sfxSlider, AudioSettingsChannel.IsSfxMuted);
        }

        private void SetSelectableTint(Selectable primaryControl, Selectable secondaryControl, bool muted)
        {
            Color tint = muted ? mutedTint : enabledTint;

            ApplyTint(primaryControl, tint);
            ApplyTint(secondaryControl, tint);
        }

        private static void ApplyTint(Selectable control, Color tint)
        {
            if (control == null)
                return;

            Graphic[] graphics = control.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics)
            {
                graphic.color = tint;
            }
        }

        private static void UpdateMuteButtonLabel(Button button, bool muted, string onText, string offText)
        {
            if (button == null)
                return;

            string text = muted ? offText : onText;

            Text legacyLabel = button.GetComponentInChildren<Text>();
            if (legacyLabel != null)
                legacyLabel.text = text;

            TMP_Text tmpLabel = button.GetComponentInChildren<TMP_Text>();
            if (tmpLabel != null)
                tmpLabel.text = text;
        }
    }
}
