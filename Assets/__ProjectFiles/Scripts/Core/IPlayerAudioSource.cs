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

        AudioClip LandSfx { get; }

        AudioClip ThrowSfx { get; }

        AudioClip SkidSfx { get; }

        AudioClip DamageSfx { get; }

        AudioClip DeathSfx { get; }

        AudioClip RespawnSfx { get; }

        event Action OnJump;

        event Action OnLand;

        event Action OnThrow;

        event Action<bool> OnSkidChanged;

        event Action<float> OnDamageTaken;

        event Action OnDeath;

        event Action OnRespawn;
    }
}