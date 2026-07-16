using System;

namespace Orpaits.Core
{
    /// <summary>
    /// Observer channel for global music and SFX settings.
    /// UI publishes changes here; AudioManager listens and applies them.
    /// </summary>
    public static class AudioSettingsChannel
    {
        private static float musicVolume = 1f;
        private static float sfxVolume = 1f;
        private static bool musicMuted;
        private static bool sfxMuted;

        public static event Action<float> MusicVolumeChanged;
        public static event Action<float> SfxVolumeChanged;
        public static event Action<bool> MusicMutedChanged;
        public static event Action<bool> SfxMutedChanged;

        public static float MusicVolume => musicVolume;
        public static float SfxVolume => sfxVolume;
        public static bool IsMusicMuted => musicMuted;
        public static bool IsSfxMuted => sfxMuted;

        public static void SetMusicVolume(float normalizedVolume)
        {
            musicVolume = Clamp01(normalizedVolume);
            MusicVolumeChanged?.Invoke(musicVolume);
        }

        public static void SetSfxVolume(float normalizedVolume)
        {
            sfxVolume = Clamp01(normalizedVolume);
            SfxVolumeChanged?.Invoke(sfxVolume);
        }

        public static void SetMusicMuted(bool muted)
        {
            musicMuted = muted;
            MusicMutedChanged?.Invoke(musicMuted);
        }

        public static void SetSfxMuted(bool muted)
        {
            sfxMuted = muted;
            SfxMutedChanged?.Invoke(sfxMuted);
        }

        public static void ToggleMusicMuted()
        {
            SetMusicMuted(!musicMuted);
        }

        public static void ToggleSfxMuted()
        {
            SetSfxMuted(!sfxMuted);
        }

        private static float Clamp01(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return 1f;

            return Math.Clamp(value, 0f, 1f);
        }
    }
}