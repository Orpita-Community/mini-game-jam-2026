using System;
using UnityEngine;

namespace Orpaits.Core
{
    /// <summary>
    /// Core-level contract for enemy audio sources observed by AudioManager.
    /// </summary>
    public interface IEnemyAudioSource
    {
        AudioClip DamageSfx { get; }

        event Action<float> OnDamageTaken;
    }
}