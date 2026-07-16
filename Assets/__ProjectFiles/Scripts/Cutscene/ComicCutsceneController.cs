using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Orpaits.Cinematics
{
    /// <summary>
    /// Drives a UI-based comic book cutscene using Unity 6 Awaitables for snappy animations.
    /// Advances on player input and loads the main game when finished.
    /// </summary>
    public class ComicCutsceneController : MonoBehaviour
    {
        [Serializable]
        public class ComicPanel
        {
            public RectTransform panelTransform;
            public CanvasGroup canvasGroup;
            [Tooltip("Where the panel starts relative to its final position before sliding in.")]
            public Vector2 slideStartOffset = new Vector2(0f, -100f); 
        }

        [Header("Panel Sequence")]
        [SerializeField] private ComicPanel[] sequence;
        
        [Header("Animation Feel")]
        [SerializeField] [Tooltip("Higher = faster snap")] private float snapSpeed = 15f;
        
        [Header("Controls")]
        [SerializeField] private InputActionReference advanceAction;

        [Header("Transition")]
        [SerializeField] [Tooltip("Name of the scene to load after the comic ends")] 
        private string gameplaySceneName = "SampleScene";

        private int currentIndex = -1;
        private bool isAnimating = false;
        private Vector2[] finalPositions;

        private void Awake()
        {
            // Store the final layout positions you set up in the Editor
            finalPositions = new Vector2[sequence.Length];
            
            for (int i = 0; i < sequence.Length; i++)
            {
                ComicPanel panel = sequence[i];
                finalPositions[i] = panel.panelTransform.anchoredPosition;
                
                // Hide all panels and push them to their starting offset
                panel.canvasGroup.alpha = 0f;
                panel.panelTransform.anchoredPosition += panel.slideStartOffset;
                panel.panelTransform.localScale = Vector3.one * 0.8f; // Start slightly shrunk
                panel.panelTransform.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (advanceAction != null)
            {
                advanceAction.action.Enable();
                advanceAction.action.performed += HandleAdvance;
            }
        }

        private void OnDisable()
        {
            if (advanceAction != null)
            {
                advanceAction.action.performed -= HandleAdvance;
                advanceAction.action.Disable();
            }
        }

        private void Start()
        {
            // Show the first panel immediately
            ShowNextPanel();
        }

        private void HandleAdvance(InputAction.CallbackContext context)
        {
            // Don't allow skipping if a panel is currently flying onto the screen
            if (isAnimating) return;
            
            ShowNextPanel();
        }

        private void ShowNextPanel()
        {
            currentIndex++;

            if (currentIndex >= sequence.Length)
            {
                // Sequence finished! Load the game.
                LoadGameplayScene();
                return;
            }

            // Start the snappy animation for the new panel
            _ = AnimatePanelAsync(sequence[currentIndex], finalPositions[currentIndex]);
        }

        private async Awaitable AnimatePanelAsync(ComicPanel panel, Vector2 targetPosition)
        {
            isAnimating = true;
            panel.panelTransform.gameObject.SetActive(true);

            float t = 0f;
            
            // Loop until the panel is practically at its destination
            while (Vector2.Distance(panel.panelTransform.anchoredPosition, targetPosition) > 0.5f)
            {
                t += Time.deltaTime * snapSpeed;
                
                // Smoothly snap position, scale, and fade in
                panel.panelTransform.anchoredPosition = Vector2.Lerp(panel.panelTransform.anchoredPosition, targetPosition, t);
                panel.panelTransform.localScale = Vector3.Lerp(panel.panelTransform.localScale, Vector3.one, t);
                panel.canvasGroup.alpha = Mathf.Lerp(panel.canvasGroup.alpha, 1f, t);

                await Awaitable.NextFrameAsync();
            }

            // Ensure final values are perfectly set
            panel.panelTransform.anchoredPosition = targetPosition;
            panel.panelTransform.localScale = Vector3.one;
            panel.canvasGroup.alpha = 1f;

            isAnimating = false;
        }

        private void LoadGameplayScene()
        {
            // Make sure your Gameplay scene is added in File -> Build Settings!
            SceneManager.LoadScene(gameplaySceneName);
        }
    }
}