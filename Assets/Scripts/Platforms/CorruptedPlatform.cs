using System;
using System.Threading;
using UnityEngine;

namespace Orpaits.Platforms
{
    public class CorruptedPlatform : BasePlatform
    {
        public enum PlatformState { Stable, Shaking, Falling, Respawning }

        [Header("Timings")]
        [SerializeField] private float shakeDuration = 1f;
        [SerializeField] private float respawnDelay = 3f;
        [SerializeField] private float fallDistance = 5f;
        [SerializeField] private float fallSpeed = 3f;

        [Header("Shake")]
        [SerializeField] private float shakeIntensity = 0.1f;
        [SerializeField] private float shakeSpeed = 20f;

        public event Action OnShakeStart;
        public event Action OnFallStart;
        public event Action OnRespawn;

        public PlatformState CurrentState { get; private set; } = PlatformState.Stable;

        private Vector2 _origin;
        private bool _triggered;
        private CancellationTokenSource _cts;

        protected override void Awake()
        {
            base.Awake();
            _origin = transform.position;
        }

        protected override void OnPlayerEnter(Collision2D collision)
        {
            if (CurrentState != PlatformState.Stable || _triggered) return;

            _triggered = true;
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _ = FallRoutine(_cts.Token);
        }

        private async Awaitable FallRoutine(CancellationToken ct)
        {
            CurrentState = PlatformState.Shaking;
            OnShakeStart?.Invoke();

            float t = 0f;
            while (t < shakeDuration)
            {
                if (ct.IsCancellationRequested) return;
                t += Time.deltaTime;
                transform.position = _origin + new Vector2(
                    Mathf.Sin(t * shakeSpeed) * shakeIntensity,
                    Mathf.Cos(t * shakeSpeed * 1.3f) * shakeIntensity * 0.5f
                );
                await Awaitable.NextFrameAsync();
            }

            transform.position = _origin;

            CurrentState = PlatformState.Falling;
            OnFallStart?.Invoke();
            platformCollider.enabled = false;

            var target = _origin + Vector2.down * fallDistance;
            var duration = fallDistance / fallSpeed;
            t = 0f;
            while (t < duration)
            {
                if (ct.IsCancellationRequested) return;
                t += Time.deltaTime;
                transform.position = Vector2.Lerp(_origin, target, Mathf.Clamp01(t / duration));
                await Awaitable.NextFrameAsync();
            }

            spriteRenderer.enabled = false;
            CurrentState = PlatformState.Respawning;
            await Awaitable.WaitForSecondsAsync(respawnDelay);
            if (ct.IsCancellationRequested) return;

            transform.position = _origin;
            spriteRenderer.enabled = true;
            platformCollider.enabled = true;
            _triggered = false;
            IsPlayerOnPlatform = false;
            CurrentState = PlatformState.Stable;
            OnRespawn?.Invoke();
        }

        public override void ResetPlatform()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            transform.position = _origin;
            spriteRenderer.enabled = true;
            platformCollider.enabled = true;
            _triggered = false;
            IsPlayerOnPlatform = false;
            CurrentState = PlatformState.Stable;
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * fallDistance);
        }
    }
}
