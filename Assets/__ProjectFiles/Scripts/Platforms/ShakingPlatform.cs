using System;
using System.Threading;
using UnityEngine;

namespace Orpaits.Platforms
{
    /// <summary>
    /// A platform that periodically shakes. Staying on it for too long may cause
    /// the player to drop some collected Windows XP icons.
    ///
    /// Design reference: mechanics-260712_2137.md §6 (Shaking Platform)
    /// </summary>
    public class ShakingPlatform : BasePlatform
    {
        [Header("Shake Cycle")]
        [SerializeField]
        [Tooltip("Time between shake events (seconds)")]
        private float shakeInterval = 3.0f;

        [SerializeField]
        [Tooltip("Duration of each shake event (seconds)")]
        private float shakeDuration = 0.6f;

        [SerializeField]
        [Tooltip("Intensity of the shake displacement")]
        private float shakeIntensity = 0.08f;

        [SerializeField]
        [Tooltip("Speed of shake oscillation")]
        private float shakeFrequency = 30.0f;

        [Header("Icon Loss")]
        [SerializeField]
        [Tooltip("Number of icons the player loses during a shake if standing on platform")]
        private int iconsLostPerShake = 1;

        [SerializeField]
        [Tooltip("Cooldown before player can lose more icons from same platform (seconds)")]
        private float iconLossCooldown = 1.5f;

        /// <summary>Fired shortly before a shake event begins (warning).</summary>
        public event Action OnShakeWarning;

        /// <summary>Fired when the shake starts.</summary>
        public event Action OnShake;

        /// <summary>Fired when the player loses icons from this platform's shake.</summary>
        public event Action OnIconsLost;

        public bool IsShaking { get; private set; }

        private Vector2 originalPosition;
        private float lastIconLossTime;
        private CancellationTokenSource cts;

        protected override void Awake()
        {
            base.Awake();
            originalPosition = transform.position;
        }

        private void Start()
        {
            cts?.Dispose();
            cts = new CancellationTokenSource();
            _ = ShakeCycleAsync(cts.Token);
        }

        private async Awaitable ShakeCycleAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && enabled)
            {
                await Awaitable.WaitForSecondsAsync(shakeInterval);
                if (ct.IsCancellationRequested || !enabled) return;

                OnShakeWarning?.Invoke();

                // Brief warning pause before shaking
                await Awaitable.WaitForSecondsAsync(0.3f);
                if (ct.IsCancellationRequested || !enabled) return;

                // Perform shake
                IsShaking = true;
                OnShake?.Invoke();

                float timer = 0f;
                while (timer < shakeDuration)
                {
                    if (ct.IsCancellationRequested) return;

                    timer += Time.deltaTime;
                    float shakeX = Mathf.Sin(timer * shakeFrequency) * shakeIntensity;
                    float shakeY = Mathf.Cos(timer * shakeFrequency * 1.7f) * shakeIntensity * 0.7f;
                    transform.position = originalPosition + new Vector2(shakeX, shakeY);
                    await Awaitable.NextFrameAsync();
                }

                // Reset position
                transform.position = originalPosition;
                IsShaking = false;

                // If player was on platform during shake, trigger icon loss
                if (IsPlayerOnPlatform && Time.time >= lastIconLossTime + iconLossCooldown)
                {
                    lastIconLossTime = Time.time;
                    OnPlayerLostIcons();
                }
            }
        }

        /// <summary>
        /// Called when the player loses icons due to this platform shaking.
        /// Fires the onIconsLost event. Actual icon removal is handled by
        /// the PlayerInventory system listening to this event.
        /// </summary>
        protected virtual void OnPlayerLostIcons()
        {
            OnIconsLost?.Invoke();
            Debug.Log($"[ShakingPlatform] Player lost {iconsLostPerShake} icon(s) on {name}");
        }

        /// <summary>
        /// Public method for other systems (e.g., PlayerInventory) to know
        /// how many icons are lost per shake.
        /// </summary>
        public int GetIconsLostPerShake() => iconsLostPerShake;

        public override void ResetPlatform()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;

            transform.position = originalPosition;
            IsShaking = false;
            lastIconLossTime = 0f;
            IsPlayerOnPlatform = false;

            // Restart the shake cycle
            if (gameObject.activeInHierarchy)
            {
                cts = new CancellationTokenSource();
                _ = ShakeCycleAsync(cts.Token);
            }
        }

        private void OnDestroy()
        {
            cts?.Cancel();
            cts?.Dispose();
        }
    }
}
