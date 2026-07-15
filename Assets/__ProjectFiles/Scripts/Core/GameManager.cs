using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Orpaits.Core
{
    /// <summary>
    /// High-level game state and scene flow for Orpaits.
    ///
    /// Persistent singleton (DontDestroyOnLoad). Owns the app-level
    /// <see cref="GameState"/>, the pause time-scale toggle, and scene
    /// transitions (Main Menu ↔ Gameplay ↔ Boss Arena). Per-scene UI
    /// controllers (menu, pause, defeat, victory) read/drive it.
    ///
    /// Design reference: issue #24 (Win/Lose Conditions &amp; Ending Sequence),
    /// issue #14 (Main Menu, Pause, Victory &amp; Defeat Screens).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        /// <summary>The single active GameManager (survives scene loads).</summary>
        public static GameManager Instance { get; private set; }

        [Header("Scene Names (must be in Build Settings)")]
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string gameplayScene = "SampleScene";
        [SerializeField] private string bossArenaScene = "BossArena";

        /// <summary>Current high-level game state.</summary>
        public GameState State { get; private set; } = GameState.Playing;

        /// <summary>Fired whenever <see cref="State"/> changes.</summary>
        public event Action<GameState> OnStateChanged;

        public string MainMenuScene => mainMenuScene;
        public string GameplayScene => gameplayScene;
        public string BossArenaScene => bossArenaScene;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            // Establish the initial state for the scene we spawned in.
            ApplyStateForScene(SceneManager.GetActiveScene().name);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                SceneManager.sceneLoaded -= HandleSceneLoaded;
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // A fresh scene always resumes normal time.
            Time.timeScale = 1f;
            ApplyStateForScene(scene.name);
        }

        private void ApplyStateForScene(string sceneName)
        {
            SetState(sceneName == mainMenuScene ? GameState.MainMenu : GameState.Playing);
        }

        /// <summary>Sets the state and notifies listeners (no-op if unchanged).</summary>
        public void SetState(GameState next)
        {
            if (State == next) return;
            State = next;
            OnStateChanged?.Invoke(next);
        }

        /// <summary>
        /// Pauses or resumes gameplay (freezes time). Only toggles from the
        /// matching state so it can't interrupt Victory/Defeat.
        /// </summary>
        public void SetPaused(bool paused)
        {
            if (paused && State != GameState.Playing) return;
            if (!paused && State != GameState.Paused) return;

            Time.timeScale = paused ? 0f : 1f;
            SetState(paused ? GameState.Paused : GameState.Playing);
        }

        // --- Scene transitions ---

        public void StartGame() => LoadScene(gameplayScene);
        public void LoadMainMenu() => LoadScene(mainMenuScene);
        public void LoadBossArena() => LoadScene(bossArenaScene);
        public void ReloadCurrentScene() => LoadScene(SceneManager.GetActiveScene().name);

        private void LoadScene(string sceneName)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>Quits the application (stops play mode in the editor).</summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
