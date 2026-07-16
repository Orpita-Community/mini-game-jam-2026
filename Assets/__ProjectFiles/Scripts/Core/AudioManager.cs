using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace Orpaits.Core
{
    /// <summary>
    /// Persistent audio hub for scene music and observed SFX playback.
    /// Attach this to a bootstrap GameObject in the first loaded scene.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Serializable]
        private struct SceneMusicEntry
        {
            [SerializeField]
            private string sceneName;

            [SerializeField]
            private AudioClip music;

            public string SceneName => sceneName;
            public AudioClip Music => music;
        }

        public static AudioManager Instance { get; private set; }

        [Header("Mixer")]
        [SerializeField]
        private AudioMixer audioMixer;

        [SerializeField]
        private bool useAudioMixerForVolume = false;

        [SerializeField]
        private AudioMixerGroup musicOutputGroup;

        [SerializeField]
        private AudioMixerGroup sfxOutputGroup;

        [SerializeField]
        private string masterVolumeParameter = "MasterVolume";

        [SerializeField]
        private string musicVolumeParameter = "MusicVolume";

        [SerializeField]
        private string sfxVolumeParameter = "SfxVolume";

        [Header("Sources")]
        [SerializeField]
        private AudioSource musicSource;

        [SerializeField]
        private AudioSource sfxSource;

        [Header("Music")]
        [SerializeField]
        private bool playMusicOnSceneLoad = true;

        [SerializeField]
        private bool loopMusic = true;

        [SerializeField]
        private AudioClip defaultMusic;

        [SerializeField]
        private SceneMusicEntry[] sceneMusic;

        private float musicVolume = 1f;
        private float sfxVolume = 1f;
        private bool musicMuted;
        private bool sfxMuted;

        private readonly Dictionary<IEnemyAudioSource, Action<float>> damageHandlers = new();
        private readonly Dictionary<IEnemyAudioSource, Action> deathHandlers = new();
        private readonly Dictionary<IBossAudioSource, Action> phaseHandlers = new();
        private readonly Dictionary<IBossAudioSource, Action> telegraphHandlers = new();
        private readonly Dictionary<IBossAudioSource, Action> shockwaveHandlers = new();
        private readonly Dictionary<IBossAudioSource, Action<float>> platformDeletionHandlers = new();
        private readonly Dictionary<IBossAudioSource, Action> bossDefeatedHandlers = new();
        private readonly Dictionary<IPlayerAudioSource, Action> playerJumpHandlers = new();
        private readonly Dictionary<IPlayerAudioSource, Action> playerLandHandlers = new();
        private readonly Dictionary<IPlayerAudioSource, Action> playerThrowHandlers = new();
        private readonly Dictionary<IPlayerAudioSource, Action<bool>> playerSkidHandlers = new();
        private readonly Dictionary<IPlayerAudioSource, Action<float>> playerDamageHandlers = new();
        private readonly Dictionary<IPlayerAudioSource, Action> playerDeathHandlers = new();
        private readonly Dictionary<IPlayerAudioSource, Action> playerRespawnHandlers = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureSources();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            AudioSettingsChannel.MusicVolumeChanged += HandleMusicVolumeChanged;
            AudioSettingsChannel.SfxVolumeChanged += HandleSfxVolumeChanged;
            AudioSettingsChannel.MusicMutedChanged += HandleMusicMutedChanged;
            AudioSettingsChannel.SfxMutedChanged += HandleSfxMutedChanged;

            ApplySettingsFromChannel();
        }

        private void Start()
        {
            RegisterExistingEnemies();

            if (playMusicOnSceneLoad)
                RefreshMusicForScene(SceneManager.GetActiveScene().name);
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            AudioSettingsChannel.MusicVolumeChanged -= HandleMusicVolumeChanged;
            AudioSettingsChannel.SfxVolumeChanged -= HandleSfxVolumeChanged;
            AudioSettingsChannel.MusicMutedChanged -= HandleMusicMutedChanged;
            AudioSettingsChannel.SfxMutedChanged -= HandleSfxMutedChanged;
        }

        private void OnDestroy()
        {
            UnregisterAllEnemies();

            if (Instance == this)
                Instance = null;
        }

        public void RegisterEnemy(IEnemyAudioSource enemy)
        {
            if (enemy == null || damageHandlers.ContainsKey(enemy))
                return;

            Action<float> damageHandler = _ =>
            {
                if (enemy.HealthNormalized > 0f)
                    TryPlaySfx(enemy.DamageSfx);
            };
            Action deathHandler = () => TryPlaySfx(enemy.DeathSfx);

            damageHandlers[enemy] = damageHandler;
            deathHandlers[enemy] = deathHandler;

            enemy.OnDamageTaken += damageHandler;
            enemy.OnDeath += deathHandler;

            if (enemy is IBossAudioSource boss)
            {
                Action phaseHandler = () => TryPlaySfx(boss.PhaseTransitionSfx);
                phaseHandlers[boss] = phaseHandler;
                boss.OnPhaseTransition += phaseHandler;

                Action telegraphHandler = () => TryPlaySfx(boss.TelegraphSfx);
                Action shockwaveHandler = () => TryPlaySfx(boss.ShockwaveSfx);
                Action<float> deletionHandler = _ => TryPlaySfx(boss.PlatformDeletionSfx);
                Action defeatedHandler = () =>
                {
                    TryPlaySfx(boss.DefeatedSfx);
                    if (boss.DefeatedMusic != null)
                        PlayMusic(boss.DefeatedMusic);
                };

                telegraphHandlers[boss] = telegraphHandler;
                shockwaveHandlers[boss] = shockwaveHandler;
                platformDeletionHandlers[boss] = deletionHandler;
                bossDefeatedHandlers[boss] = defeatedHandler;

                boss.OnAttackTelegraph += telegraphHandler;
                boss.OnShockwave += shockwaveHandler;
                boss.OnPlatformDeletion += deletionHandler;
                boss.OnBossDefeated += defeatedHandler;
            }
        }

        public void RegisterPlayer(IPlayerAudioSource player)
        {
            if (player == null || playerJumpHandlers.ContainsKey(player))
                return;

            Action jumpHandler = () => TryPlaySfx(player.JumpSfx);
            Action landHandler = () => TryPlaySfx(player.LandSfx);
            Action throwHandler = () => TryPlaySfx(player.ThrowSfx);
            Action<bool> skidHandler = isSkidding =>
            {
                if (isSkidding)
                    TryPlaySfx(player.SkidSfx);
            };
            Action<float> damageHandler = _ => TryPlaySfx(player.DamageSfx);
            Action deathHandler = () => TryPlaySfx(player.DeathSfx);
            Action respawnHandler = () => TryPlaySfx(player.RespawnSfx);

            playerJumpHandlers[player] = jumpHandler;
            playerLandHandlers[player] = landHandler;
            playerThrowHandlers[player] = throwHandler;
            playerSkidHandlers[player] = skidHandler;
            playerDamageHandlers[player] = damageHandler;
            playerDeathHandlers[player] = deathHandler;
            playerRespawnHandlers[player] = respawnHandler;

            player.OnJump += jumpHandler;
            player.OnLand += landHandler;
            player.OnThrow += throwHandler;
            player.OnSkidChanged += skidHandler;
            player.OnDamageTaken += damageHandler;
            player.OnDeath += deathHandler;
            player.OnRespawn += respawnHandler;
        }

        public void UnregisterEnemy(IEnemyAudioSource enemy)
        {
            if (enemy == null)
                return;

            if (damageHandlers.TryGetValue(enemy, out var damageHandler))
            {
                enemy.OnDamageTaken -= damageHandler;
                damageHandlers.Remove(enemy);
            }

            if (deathHandlers.TryGetValue(enemy, out var deathHandler))
            {
                enemy.OnDeath -= deathHandler;
                deathHandlers.Remove(enemy);
            }

            if (enemy is IBossAudioSource boss)
            {
                if (phaseHandlers.TryGetValue(boss, out var phaseHandler))
                {
                    boss.OnPhaseTransition -= phaseHandler;
                    phaseHandlers.Remove(boss);
                }

                if (telegraphHandlers.TryGetValue(boss, out var telegraphHandler))
                {
                    boss.OnAttackTelegraph -= telegraphHandler;
                    telegraphHandlers.Remove(boss);
                }

                if (shockwaveHandlers.TryGetValue(boss, out var shockwaveHandler))
                {
                    boss.OnShockwave -= shockwaveHandler;
                    shockwaveHandlers.Remove(boss);
                }

                if (platformDeletionHandlers.TryGetValue(boss, out var deletionHandler))
                {
                    boss.OnPlatformDeletion -= deletionHandler;
                    platformDeletionHandlers.Remove(boss);
                }

                if (bossDefeatedHandlers.TryGetValue(boss, out var defeatedHandler))
                {
                    boss.OnBossDefeated -= defeatedHandler;
                    bossDefeatedHandlers.Remove(boss);
                }
            }
        }

        public void UnregisterPlayer(IPlayerAudioSource player)
        {
            if (player == null)
                return;

            if (playerJumpHandlers.TryGetValue(player, out var jumpHandler))
            {
                player.OnJump -= jumpHandler;
                playerJumpHandlers.Remove(player);
            }

            if (playerLandHandlers.TryGetValue(player, out var landHandler))
            {
                player.OnLand -= landHandler;
                playerLandHandlers.Remove(player);
            }

            if (playerThrowHandlers.TryGetValue(player, out var throwHandler))
            {
                player.OnThrow -= throwHandler;
                playerThrowHandlers.Remove(player);
            }

            if (playerSkidHandlers.TryGetValue(player, out var skidHandler))
            {
                player.OnSkidChanged -= skidHandler;
                playerSkidHandlers.Remove(player);
            }

            if (playerDamageHandlers.TryGetValue(player, out var damageHandler))
            {
                player.OnDamageTaken -= damageHandler;
                playerDamageHandlers.Remove(player);
            }

            if (playerDeathHandlers.TryGetValue(player, out var deathHandler))
            {
                player.OnDeath -= deathHandler;
                playerDeathHandlers.Remove(player);
            }

            if (playerRespawnHandlers.TryGetValue(player, out var respawnHandler))
            {
                player.OnRespawn -= respawnHandler;
                playerRespawnHandlers.Remove(player);
            }
        }

        public void PlayMusic(AudioClip clip, bool restart = true)
        {
            if (musicSource == null || clip == null)
                return;

            if (!restart && musicSource.isPlaying && musicSource.clip == clip)
                return;

            musicSource.clip = clip;
            musicSource.loop = loopMusic;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource == null)
                return;

            musicSource.Stop();
            musicSource.clip = null;
        }

        public void SetMasterVolume(float normalizedVolume)
        {
            SetMixerVolume(masterVolumeParameter, normalizedVolume);
        }

        public void SetMusicVolume(float normalizedVolume)
        {
            AudioSettingsChannel.SetMusicVolume(normalizedVolume);
        }

        public void SetSfxVolume(float normalizedVolume)
        {
            AudioSettingsChannel.SetSfxVolume(normalizedVolume);
        }

        public void SetMusicMuted(bool muted)
        {
            AudioSettingsChannel.SetMusicMuted(muted);
        }

        public void SetSfxMuted(bool muted)
        {
            AudioSettingsChannel.SetSfxMuted(muted);
        }

        public bool IsMusicMuted => musicMuted;

        public bool IsSfxMuted => sfxMuted;

        public float MusicVolume => musicVolume;

        public float SfxVolume => sfxVolume;

        public void ToggleMusicMuted()
        {
            AudioSettingsChannel.ToggleMusicMuted();
        }

        public void ToggleSfxMuted()
        {
            AudioSettingsChannel.ToggleSfxMuted();
        }

        public void PlaySfx(AudioClip clip)
        {
            TryPlaySfx(clip);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RegisterExistingEnemies();

            if (playMusicOnSceneLoad)
                RefreshMusicForScene(scene.name);
        }

        private void RefreshMusicForScene(string sceneName)
        {
            AudioClip music = GetMusicForScene(sceneName);

            if (music != null)
                PlayMusic(music, restart: false);
            else
                StopMusic();
        }

        private AudioClip GetMusicForScene(string sceneName)
        {
            if (sceneMusic != null)
            {
                foreach (SceneMusicEntry entry in sceneMusic)
                {
                    if (!string.IsNullOrWhiteSpace(entry.SceneName) &&
                        string.Equals(entry.SceneName, sceneName, StringComparison.OrdinalIgnoreCase) &&
                        entry.Music != null)
                    {
                        return entry.Music;
                    }
                }
            }

            return defaultMusic;
        }

        private void RegisterExistingEnemies()
        {
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IEnemyAudioSource enemy)
                    RegisterEnemy(enemy);

                if (behaviour is IPlayerAudioSource player)
                    RegisterPlayer(player);
            }
        }

        private void UnregisterAllEnemies()
        {
            foreach (IEnemyAudioSource enemy in new List<IEnemyAudioSource>(damageHandlers.Keys))
            {
                UnregisterEnemy(enemy);
            }
        }

        private void TryPlaySfx(AudioClip clip)
        {
            if (sfxSource == null || clip == null)
                return;

            sfxSource.PlayOneShot(clip);
        }

        private void EnsureSources()
        {
            if (musicSource == null || musicSource.transform.parent != transform)
                musicSource = CreateManagedSource("MusicSource");

            if (sfxSource == null || sfxSource.transform.parent != transform)
                sfxSource = CreateManagedSource("SfxSource");

            musicSource.playOnAwake = false;
            musicSource.loop = loopMusic;
            musicSource.spatialBlend = 0f;
            musicSource.outputAudioMixerGroup = musicOutputGroup;

            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.outputAudioMixerGroup = sfxOutputGroup;

            ApplyMusicVolume();
            ApplySfxVolume();
        }

        private void ApplySettingsFromChannel()
        {
            musicVolume = AudioSettingsChannel.MusicVolume;
            sfxVolume = AudioSettingsChannel.SfxVolume;
            musicMuted = AudioSettingsChannel.IsMusicMuted;
            sfxMuted = AudioSettingsChannel.IsSfxMuted;

            ApplyMusicVolume();
            ApplySfxVolume();
        }

        private void HandleMusicVolumeChanged(float volume)
        {
            musicVolume = volume;
            ApplyMusicVolume();
        }

        private void HandleSfxVolumeChanged(float volume)
        {
            sfxVolume = volume;
            ApplySfxVolume();
        }

        private void HandleMusicMutedChanged(bool muted)
        {
            musicMuted = muted;
            ApplyMusicVolume();
        }

        private void HandleSfxMutedChanged(bool muted)
        {
            sfxMuted = muted;
            ApplySfxVolume();
        }

        private void ApplyMusicVolume()
        {
            float appliedVolume = musicMuted ? 0f : musicVolume;
            if (musicSource != null)
                musicSource.volume = appliedVolume;

            if (useAudioMixerForVolume)
                SetMixerVolume(musicVolumeParameter, appliedVolume);
        }

        private void ApplySfxVolume()
        {
            float appliedVolume = sfxMuted ? 0f : sfxVolume;
            if (sfxSource != null)
                sfxSource.volume = appliedVolume;

            if (useAudioMixerForVolume)
                SetMixerVolume(sfxVolumeParameter, appliedVolume);
        }

        private AudioSource CreateManagedSource(string sourceName)
        {
            GameObject sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);
            sourceObject.hideFlags = HideFlags.HideInHierarchy;

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        private void SetMixerVolume(string parameterName, float normalizedVolume)
        {
            if (audioMixer == null || string.IsNullOrWhiteSpace(parameterName))
                return;

            float clamped = Mathf.Clamp01(normalizedVolume);
            float decibels = clamped <= 0.0001f ? -80f : Mathf.Log10(clamped) * 20f;

            try
            {
                audioMixer.SetFloat(parameterName, decibels);
            }
            catch (ArgumentException)
            {
                Debug.LogWarning($"[AudioManager] Mixer parameter '{parameterName}' is not exposed. Falling back to AudioSource volume only.");
                useAudioMixerForVolume = false;
            }
        }
    }
}