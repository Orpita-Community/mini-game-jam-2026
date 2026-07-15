using System;
using UnityEngine;

namespace Orpaits.Core
{
    /// <summary>
    /// Core-level contract for enemy audio sources observed by AudioManager.
    /// </summary>
    public interface IEnemyAudioSource
    {
        float HealthNormalized { get; }

        AudioClip DamageSfx { get; }

        AudioClip DeathSfx { get; }

        AudioClip PhaseTransitionSfx { get; }

        event Action<float> OnDamageTaken;

        event Action OnDeath;
    }
}