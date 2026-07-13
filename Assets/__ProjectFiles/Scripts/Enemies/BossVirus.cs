using System;
using System.Threading;
using UnityEngine;

namespace Orpaits.Enemies
{
    /// <summary>
    /// The final Boss Virus — a 3-phase boss fight at the climax of the game.
    ///
    /// Phases:
    ///   Phase 1 (100%-66% HP) — The Spam: throws corrupted file projectiles.
    ///   Phase 2 (66%-33% HP)  — The Deletion: converts stable platforms to falling.
    ///   Phase 3 (33%-0% HP)   — Glitch Wave: massive shockwave attack.
    ///
    /// Design reference: level-design-260712_2153.md (Boss Fight Details),
    /// game-loop-260712_2137.md (Combat Phase)
    /// </summary>
    public class BossVirus : BaseEnemy
    {
        public enum BossPhase
        {
            Phase1,
            Phase2,
            Phase3
        }

        [Header("Boss Settings")]
        [SerializeField]
        private string bossName = "THE VIRUS";

        [Header("Projectile Attack (Phase 1)")]
        [SerializeField]
        private GameObject corruptedProjectilePrefab;

        [SerializeField]
        private Transform[] projectileSpawnPoints;

        [SerializeField]
        [Tooltip("Time between projectile volleys (seconds)")]
        private float projectileInterval = 2.5f;

        [Header("Platform Deletion (Phase 2)")]
        [SerializeField]
        [Tooltip("Percentage of stable platforms to convert to falling (0..1)")]
        private float deletionPercentage = 0.30f;

        [Header("Glitch Wave (Phase 3)")]
        [SerializeField]
        [Tooltip("How long the shockwave telegraph lasts before the wave fires")]
        private float shockwaveTelegraphDuration = 1.5f;

        [Header("Phase Thresholds")]
        [SerializeField]
        [Tooltip("HP % at which Phase 2 begins")]
        private float phase2Threshold = 0.66f;

        [SerializeField]
        [Tooltip("HP % at which Phase 3 begins")]
        private float phase3Threshold = 0.33f;

        [Header("Battle Arena")]
        [SerializeField]
        private LayerMask platformLayer;

        /// <summary>Fired when the boss transitions to a new phase.</summary>
        public event Action<BossPhase> OnPhaseTransition;

        /// <summary>Fired when the boss telegraphs an attack (visual cue frame).</summary>
        public event Action OnAttackTelegraph;

        /// <summary>Fired when the glitch wave shockwave fires (Phase 3).</summary>
        public event Action OnShockwave;

        /// <summary>Fired when platforms should be deleted (Phase 2 transition).</summary>
        public event Action<float> OnPlatformDeletion;

        /// <summary>Fired when the boss is defeated and the arena should unlock.</summary>
        public event Action OnBossDefeated;

        public BossPhase CurrentPhase { get; private set; } = BossPhase.Phase1;
        public bool IsBossActive { get; private set; }

        private CancellationTokenSource attackCts;
        private CancellationTokenSource lifecycleCts;

        protected override void Awake()
        {
            base.Awake();
            CurrentPhase = BossPhase.Phase1;
        }

        /// <summary>
        /// Start the boss fight.
        /// Called when the player enters the boss arena.
        /// </summary>
        public void StartBossFight()
        {
            if (IsBossActive || IsDead) return;

            IsBossActive = true;
            lifecycleCts?.Dispose();
            lifecycleCts = new CancellationTokenSource();
            _ = BossRoutineAsync(lifecycleCts.Token);
        }

        private async Awaitable BossRoutineAsync(CancellationToken ct)
        {
            // Small delay before first attack
            await Awaitable.WaitForSecondsAsync(1f, ct);

            while (!ct.IsCancellationRequested && !IsDead)
            {
                switch (CurrentPhase)
                {
                    case BossPhase.Phase1:
                        await Phase1AttackRoutineAsync(ct);
                        break;
                    case BossPhase.Phase2:
                        await Phase2AttackRoutineAsync(ct);
                        break;
                    case BossPhase.Phase3:
                        await Phase3AttackRoutineAsync(ct);
                        break;
                }

                if (ct.IsCancellationRequested) return;
            }
        }

        // ── Phase 1: The Spam ──────────────────────────────────────────────

        private async Awaitable Phase1AttackRoutineAsync(CancellationToken ct)
        {
            while (CurrentPhase == BossPhase.Phase1 && !ct.IsCancellationRequested)
            {
                OnAttackTelegraph?.Invoke();
                await Awaitable.WaitForSecondsAsync(0.3f, ct);
                if (ct.IsCancellationRequested) return;

                FireProjectileVolley();
                await Awaitable.WaitForSecondsAsync(projectileInterval, ct);
            }
        }

        private void FireProjectileVolley()
        {
            if (corruptedProjectilePrefab == null || projectileSpawnPoints == null)
                return;

            foreach (var spawn in projectileSpawnPoints)
            {
                if (spawn == null) continue;

                GameObject proj = Instantiate(corruptedProjectilePrefab, spawn.position, Quaternion.identity);
                if (proj.TryGetComponent<CorruptedProjectile>(out var cp))
                {
                    // Aim toward the player (or fallback to downward)
                    Vector2 targetDir = Vector2.down;
                    cp.Launch(targetDir);
                }
            }
        }

        // ── Phase 2: The Deletion ──────────────────────────────────────────

        private async Awaitable Phase2AttackRoutineAsync(CancellationToken ct)
        {
            // Fire projectiles AND trigger platform deletions periodically
            while (CurrentPhase == BossPhase.Phase2 && !ct.IsCancellationRequested)
            {
                OnAttackTelegraph?.Invoke();
                await Awaitable.WaitForSecondsAsync(0.3f, ct);
                if (ct.IsCancellationRequested) return;

                FireProjectileVolley();

                // Every other volley, trigger a platform deletion
                OnPlatformDeletion?.Invoke(deletionPercentage);

                await Awaitable.WaitForSecondsAsync(projectileInterval * 1.5f, ct);
            }
        }

        // ── Phase 3: Glitch Wave ──────────────────────────────────────────

        private async Awaitable Phase3AttackRoutineAsync(CancellationToken ct)
        {
            while (CurrentPhase == BossPhase.Phase3 && !ct.IsCancellationRequested)
            {
                // Telegraph the shockwave
                OnAttackTelegraph?.Invoke();
                await Awaitable.WaitForSecondsAsync(shockwaveTelegraphDuration, ct);
                if (ct.IsCancellationRequested) return;

                // Fire the shockwave
                OnShockwave?.Invoke();

                // Brief recovery, then projectiles
                await Awaitable.WaitForSecondsAsync(0.5f, ct);
                if (ct.IsCancellationRequested) return;

                FireProjectileVolley();
                await Awaitable.WaitForSecondsAsync(projectileInterval, ct);
            }
        }

        // ── Phase Transitions ──────────────────────────────────────────────

        /// <summary>
        /// Activate boss fight and jump to a specific phase for testing.
        /// Right-click the BossVirus component header in the Inspector → Context Menu.
        /// </summary>
        [ContextMenu("Force Phase 1 (The Spam)")]
        private void DebugForcePhase1()
        {
            if (IsDead) { Debug.LogWarning("[BossVirus] Boss is dead — reset first."); return; }
            ForceStartBossFight();
            Heal(maxHealth); // full HP
            TransitionToPhase(BossPhase.Phase1);
            Debug.Log("[BossVirus] 🎮 Debug: forced Phase 1 (The Spam)");
        }

        [ContextMenu("Force Phase 2 (The Deletion)")]
        private void DebugForcePhase2()
        {
            if (IsDead) { Debug.LogWarning("[BossVirus] Boss is dead — reset first."); return; }
            ForceStartBossFight();
            // Set health between phase2 and phase3 thresholds (~50%)
            float hp = Mathf.Lerp(maxHealth * phase3Threshold, maxHealth * phase2Threshold, 0.5f);
            CurrentHealth = hp;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            TransitionToPhase(BossPhase.Phase2);
            Debug.Log("[BossVirus] 🎮 Debug: forced Phase 2 (The Deletion)");
        }

        [ContextMenu("Force Phase 3 (Glitch Wave)")]
        private void DebugForcePhase3()
        {
            if (IsDead) { Debug.LogWarning("[BossVirus] Boss is dead — reset first."); return; }
            ForceStartBossFight();
            // Set health below phase3 threshold (~20%)
            float hp = maxHealth * (phase3Threshold * 0.6f);
            CurrentHealth = hp;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            TransitionToPhase(BossPhase.Phase3);
            Debug.Log("[BossVirus] 🎮 Debug: forced Phase 3 (Glitch Wave)");
        }

        [ContextMenu("Reset Boss")]
        private void DebugReset()
        {
            ResetEnemy();
            Heal(maxHealth);
            Debug.Log("[BossVirus] 🔄 Debug: boss reset to Phase 1");
        }

        private void ForceStartBossFight()
        {
            if (IsBossActive) return;
            IsBossActive = true;
            lifecycleCts?.Dispose();
            lifecycleCts = new CancellationTokenSource();
            _ = BossRoutineAsync(lifecycleCts.Token);
        }

        public override bool TakeDamage(float amount)
        {
            if (IsDead) return false;

            float previousHealth = CurrentHealth;
            bool result = base.TakeDamage(amount);

            if (result)
            {
                CheckPhaseTransition();
                CheckDeath();
            }

            return result;
        }

        private void CheckPhaseTransition()
        {
            float normalized = HealthNormalized;

            if (normalized <= phase3Threshold && CurrentPhase != BossPhase.Phase3)
            {
                TransitionToPhase(BossPhase.Phase3);
            }
            else if (normalized <= phase2Threshold && CurrentPhase != BossPhase.Phase2)
            {
                TransitionToPhase(BossPhase.Phase2);
            }
        }

        private void TransitionToPhase(BossPhase newPhase)
        {
            CurrentPhase = newPhase;
            OnPhaseTransition?.Invoke(newPhase);

            switch (newPhase)
            {
                case BossPhase.Phase2:
                    // Initial platform deletion when entering Phase 2
                    OnPlatformDeletion?.Invoke(deletionPercentage);
                    break;

                case BossPhase.Phase3:
                    // Cancel existing attack loop so Phase3 routine takes over
                    attackCts?.Cancel();
                    attackCts?.Dispose();
                    attackCts = new CancellationTokenSource();
                    _ = Phase3AttackRoutineAsync(attackCts.Token);
                    break;
            }
        }

        private void CheckDeath()
        {
            if (IsDead && HealthNormalized <= 0f)
                Die();
        }

        protected override void Die()
        {
            if (IsDead) return;

            lifecycleCts?.Cancel();
            attackCts?.Cancel();
            IsBossActive = false;

            // Boss death sequence
            OnBossDefeated?.Invoke();
            base.Die();

            // Boss stays in scene for death animation, then handled by game manager
            Debug.Log($"[BossVirus] {bossName} defeated — system restored.");
        }

        public override void OnProjectileHit(float damage)
        {
            TakeDamage(damage);
        }

        public override void ResetEnemy()
        {
            base.ResetEnemy();
            CurrentPhase = BossPhase.Phase1;
            IsBossActive = false;
        }

        private void OnDestroy()
        {
            lifecycleCts?.Cancel();
            lifecycleCts?.Dispose();
            attackCts?.Cancel();
            attackCts?.Dispose();
        }
    }
}
