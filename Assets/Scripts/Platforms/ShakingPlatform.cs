using System;
using System.Threading;
using UnityEngine;

namespace Orpaits.Platforms
{
    public class ShakingPlatform : BasePlatform
    {
        [Header("Shake Cycle")]
        [SerializeField] private float shakeInterval = 3f;
        [SerializeField] private float shakeDuration = 0.6f;
        [SerializeField] private float shakeIntensity = 0.08f;
        [SerializeField] private float shakeFrequency = 30f;

        [Header("Icon Loss")]
        [SerializeField] private int iconsLostPerShake = 1;
        [SerializeField] private float iconLossCooldown = 1.5f;

        public event Action OnShakeWarning;
        public event Action OnShake;
        public event Action OnIconsLost;

        public bool IsShaking { get; private set; }
        public int IconsLostPerShake => iconsLostPerShake;

        private Vector2 _origin;
        private float _lastLossTime;
        private CancellationTokenSource _cts;

        protected override void Awake()
        {
            base.Awake();
            _origin = transform.position;
        }

        private void Start()
        {
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _ = ShakeLoop(_cts.Token);
        }

        private async Awaitable ShakeLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && enabled)
            {
                await Awaitable.WaitForSecondsAsync(shakeInterval);
                if (ct.IsCancellationRequested || !enabled) return;

                OnShakeWarning?.Invoke();
                await Awaitable.WaitForSecondsAsync(0.3f);
                if (ct.IsCancellationRequested || !enabled) return;

                IsShaking = true;
                OnShake?.Invoke();

                float t = 0f;
                while (t < shakeDuration)
                {
                    if (ct.IsCancellationRequested) return;
                    t += Time.deltaTime;
                    transform.position = _origin + new Vector2(
                        Mathf.Sin(t * shakeFrequency) * shakeIntensity,
                        Mathf.Cos(t * shakeFrequency * 1.7f) * shakeIntensity * 0.7f
                    );
                    await Awaitable.NextFrameAsync();
                }

                transform.position = _origin;
                IsShaking = false;

                if (IsPlayerOnPlatform && Time.time >= _lastLossTime + iconLossCooldown)
                {
                    _lastLossTime = Time.time;
                    OnIconsLost?.Invoke();
                }
            }
        }

        public override void ResetPlatform()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            transform.position = _origin;
            IsShaking = false;
            _lastLossTime = 0f;
            IsPlayerOnPlatform = false;

            if (gameObject.activeInHierarchy)
            {
                _cts = new CancellationTokenSource();
                _ = ShakeLoop(_cts.Token);
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
