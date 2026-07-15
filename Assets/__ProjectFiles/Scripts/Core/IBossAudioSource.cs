using System;
using UnityEngine;

namespace Orpaits.Core
{
    /// <summary>
    /// Core-level contract for boss-specific audio cues.
    /// </summary>
    public interface IBossAudioSource : IEnemyAudioSource
    {
        AudioClip TelegraphSfx { get; }

        AudioClip ShockwaveSfx { get; }

        AudioClip PlatformDeletionSfx { get; }

        AudioClip DefeatedSfx { get; }

        AudioClip DefeatedMusic { get; }

        event Action OnPhaseTransition;

        event Action OnAttackTelegraph;

        event Action OnShockwave;

        event Action<float> OnPlatformDeletion;

        event Action OnBossDefeated;
    }
}