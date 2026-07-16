using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Orpaits.Cinematics
{
    [RequireComponent(typeof(Camera))]
    public class ComicCameraController : MonoBehaviour
    {
        [Serializable]
        public class ComicPanel
        {
            public Transform focusPoint;
            public float cameraZoom = 5f;
            public AudioClip panelAudio;
        }

        [Header("Comic Sequence")]
        [SerializeField] private ComicPanel[] sequence;

        [Header("Camera Feel")]
        [SerializeField] private float defaultSmoothTime = 0.3f;
        
        [Header("Final Transition (The Dive)")]
        [SerializeField] [Tooltip("How tight the camera zooms in on the final button press")] 
        private float finalZoomSize = 1.5f;
        [SerializeField] [Tooltip("How smooth/slow the final zoom is")] 
        private float finalZoomSmoothTime = 2.5f;
        [Header("Transition Timings")]
        [SerializeField] [Tooltip("How long it takes to reach maximum pixelation")] 
        private float pixelationDuration = 2f;
        [SerializeField] [Tooltip("Draw a curve that shoots up fast, then levels off, to fix the visual pacing!")]
        private AnimationCurve pixelationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] [Tooltip("How long the screen stays fully pixelated while the black screen fades in")] 
        private float blackFadeDuration = 1f;

        [SerializeField] private Material transitionMaterial;
        [SerializeField] [Tooltip("The Canvas Group on the Black Screen UI image")]
        private CanvasGroup fadeOverlay;

        [Header("Audio")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Controls")]
        [SerializeField] private InputActionReference advanceAction;

        [Header("Scene Loading")]
        [SerializeField] private string nextSceneName = "SampleScene";

        private Camera cam;
        private int currentIndex = -1;
        private bool isTransitioning = false;

        private Vector3 targetPosition;
        private float targetZoom;
        private Vector3 positionVelocity = Vector3.zero;
        private float zoomVelocity = 0f;
        private float currentSmoothTime;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            
            targetPosition = transform.position;
            targetZoom = cam.orthographicSize;
            currentSmoothTime = defaultSmoothTime;

            // Reset effects on awake
            if (transitionMaterial != null) transitionMaterial.SetFloat("_Progress", 0f);
            if (fadeOverlay != null) fadeOverlay.alpha = 0f;
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
            
            if (transitionMaterial != null) transitionMaterial.SetFloat("_Progress", 0f);
        }

        private void Start()
        {
            if (bgmSource != null && bgmSource.clip != null)
            {
                bgmSource.loop = true;
                bgmSource.Play();
            }
            
            ShowNextPanel();
        }

        private void LateUpdate()
        {
            if (currentIndex < 0 || currentIndex >= sequence.Length) return;

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref positionVelocity, currentSmoothTime);
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetZoom, ref zoomVelocity, currentSmoothTime);
        }

        private void HandleAdvance(InputAction.CallbackContext context)
        {
            if (isTransitioning) return;
            
            if (currentIndex >= sequence.Length - 1)
            {
                _ = PlayFinalTransitionAsync();
            }
            else
            {
                ShowNextPanel();
            }
        }

        private void ShowNextPanel()
        {
            currentIndex++;
            SetupPanelTarget(sequence[currentIndex]);
        }

        private void SetupPanelTarget(ComicPanel currentPanel)
        {
            if (currentPanel.focusPoint != null)
            {
                targetPosition = new Vector3(
                    currentPanel.focusPoint.position.x, 
                    currentPanel.focusPoint.position.y, 
                    transform.position.z
                );
                targetZoom = currentPanel.cameraZoom;
            }

            if (currentPanel.panelAudio != null && sfxSource != null)
            {
                sfxSource.Stop();
                sfxSource.PlayOneShot(currentPanel.panelAudio);
            }
        }

        private async Awaitable PlayFinalTransitionAsync()
        {
            isTransitioning = true;
            
            // Kick off the slow zoom
            currentSmoothTime = finalZoomSmoothTime;
            targetZoom = finalZoomSize;

            float startVolume = bgmSource != null ? bgmSource.volume : 0f;

            // ==========================================
            // PHASE 1: Crunch the Pixels
            // ==========================================
            float timer = 0f;
            while (timer < pixelationDuration)
            {
                timer += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(timer / pixelationDuration);
                
                if (transitionMaterial != null)
                {
                    // Evaluate the curve so the pixels crunch exactly how you draw them in the Inspector
                    float curvedProgress = pixelationCurve.Evaluate(normalizedTime);
                    transitionMaterial.SetFloat("_Progress", curvedProgress);
                }

                // Fade the music halfway down during the pixelation
                if (bgmSource != null)
                {
                    bgmSource.volume = Mathf.Lerp(startVolume, startVolume * 0.4f, normalizedTime);
                }

                await Awaitable.NextFrameAsync();
            }

            // Ensure it locks to 100% pixelated at the end of phase 1
            if (transitionMaterial != null) transitionMaterial.SetFloat("_Progress", 1f);

            // ==========================================
            // PHASE 2: Hold Pixelation and Fade to Black
            // ==========================================
            timer = 0f;
            while (timer < blackFadeDuration)
            {
                timer += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(timer / blackFadeDuration);
                
                if (fadeOverlay != null)
                {
                    fadeOverlay.alpha = normalizedTime;
                }

                // Fade the music the rest of the way to zero
                if (bgmSource != null)
                {
                    bgmSource.volume = Mathf.Lerp(startVolume * 0.4f, 0f, normalizedTime);
                }

                await Awaitable.NextFrameAsync();
            }

            // Safety lock before loading
            if (fadeOverlay != null) fadeOverlay.alpha = 1f;
            if (bgmSource != null) bgmSource.volume = 0f;

            SceneManager.LoadScene(nextSceneName);
        }
    }
}