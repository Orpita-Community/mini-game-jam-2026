using System;
using UnityEngine;

namespace Orpaits.Core
{
    /// <summary>
    /// Core-level contract for player audio cues observed by AudioManager.
    /// </summary>
    public interface IPlayerAudioSource
    {
        AudioClip JumpSfx { get; }

        AudioClip HitHurtSfx { get; }

        AudioClip GameOverSfx { get; }

        AudioClip ThrowDiskSfx { get; }

        event Action OnJump;

        event Action OnThrow;

        event Action<float> OnDamageTaken;

        event Action OnDeath;
    }
}