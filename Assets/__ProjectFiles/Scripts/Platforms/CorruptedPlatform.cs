using System;
using System.Threading;
using UnityEngine;

namespace Orpaits.Platforms
{
    /// <summary>
    /// A corrupted file platform that collapses a short time after the player steps on it.
    ///
    /// States:
    ///   Stable    → Platform is safe and static.
    ///   Shaking   → Player stepped on it; shake warning before fall.
    ///   Falling   → Platform descends/drops; player falls with it if still riding.
    ///   Respawning → After a delay, platform resets to original position.
    ///
    /// Design reference: mechanics-260712_2137.md §5 (Corrupted Platform),
    /// level-design-260712_2153.md (Falling Platform)
    /// </summary>
    public class CorruptedPlatform : BasePlatform
    {
        public enum PlatformState
        {
            Stable,
            Shaking,
            Falling,
            Respawning
        }

        [Header("Corrupted Platform Timings")]
        [SerializeField]
        [Tooltip("Time in seconds the platform shakes before falling")]
        private float shakeDuration = 1.0f;

        [SerializeField]
        [Tooltip("Time in seconds the platform is gone before respawning")]
        private float respawnDelay = 3.0f;

        [SerializeField]
        [Tooltip("How far the platform falls before disappearing (world units)")]
        private float fallDistance = 5.0f;

        [SerializeField]
        [Tooltip("Speed of the falling motion")]
        private float fallSpeed = 3.0f;

        [Header("Shake Settings")]
        [SerializeField]
        [Tooltip("Intensity of the shaking effect")]
        private float shakeIntensity = 0.1f;

        [SerializeField]
        [Tooltip("Speed of the shake oscillation")]
        private float shakeSpeed = 20.0f;

        /// <summary>Fired when the platform begins shaking (warning phase).</summary>
        public event Action OnShakeStart;

        /// <summary>Fired when the platform starts falling.</summary>
        public event Action OnFallStart;

        /// <summary>Fired when the platform respawns and returns to Stable state.</summary>
        public event Action OnRespawn;

        public PlatformState CurrentState { get; private set; } = PlatformState.Stable;

        private Vector2 originalPosition;
        private bool hasBeenTriggered;
        private CancellationTokenSource cts;

        protected override void Awake()
        {
            base.Awake();
            originalPosition = transform.position;
        }

        protected override void OnPlayerEnter(Collision2D collision)
        {
            Debug.Log("OnPlayerEnter");
            if (CurrentState != PlatformState.Stable || hasBeenTriggered)
                return;

            hasBeenTriggered = true;
            cts?.Dispose();
            cts = new CancellationTokenSource();
            _ = ShakeThenFallAsync(cts.Token);
        }

        private async Awaitable ShakeThenFallAsync(CancellationToken ct)
        {
            // Phase 1: Shaking
            CurrentState = PlatformState.Shaking;
            OnShakeStart?.Invoke();

            float shakeTimer = 0f;
            while (shakeTimer < shakeDuration)
            {
                if (ct.IsCancellationRequested) return;

                shakeTimer += Time.deltaTime;
                float shakeX = Mathf.Sin(shakeTimer * shakeSpeed) * shakeIntensity;
                float shakeY = Mathf.Cos(shakeTimer * shakeSpeed * 1.3f) * shakeIntensity * 0.5f;
                transform.position = originalPosition + new Vector2(shakeX, shakeY);
                await Awaitable.NextFrameAsync(ct);
            }

            // Reset position after shaking
            transform.position = originalPosition;

            // Phase 2: Falling
            CurrentState = PlatformState.Falling;
            OnFallStart?.Invoke();

            Vector2 fallTarget = originalPosition + Vector2.down * fallDistance;
            float fallTimer = 0f;
            float fallDuration = fallDistance / fallSpeed;

            // Disable collider so player falls through
            platformCollider.enabled = false;

            while (fallTimer < fallDuration)
            {
                if (ct.IsCancellationRequested) return;

                fallTimer += Time.deltaTime;
                float t = Mathf.Clamp01(fallTimer / fallDuration);
                transform.position = Vector2.Lerp(originalPosition, fallTarget, t);
                await Awaitable.NextFrameAsync(ct);
            }

            // Hide platform
            spriteRenderer.enabled = false;
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }

            // Phase 3: Respawning
            CurrentState = PlatformState.Respawning;
            await Awaitable.WaitForSecondsAsync(respawnDelay, ct);
            if (ct.IsCancellationRequested) return;

            // Reset everything
            transform.position = originalPosition;
            spriteRenderer.enabled = true;
            foreach (Transform child in transform)
                child.gameObject.SetActive(true);
            platformCollider.enabled = true;
            hasBeenTriggered = false;
            IsPlayerOnPlatform = false;

            CurrentState = PlatformState.Stable;
            OnRespawn?.Invoke();
        }

        public override void ResetPlatform()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;

            // Reset to initial state immediately
            transform.position = originalPosition;
            spriteRenderer.enabled = true;
            platformCollider.enabled = true;
            hasBeenTriggered = false;
            IsPlayerOnPlatform = false;
            CurrentState = PlatformState.Stable;
        }

        private void OnDestroy()
        {
            cts?.Cancel();
            cts?.Dispose();
        }

        private void OnDrawGizmosSelected()
        {
            // Visualize fall distance in editor
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * fallDistance);
        }
    }
}
