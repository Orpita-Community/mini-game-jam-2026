using System;
using UnityEngine;

namespace Orpaits.Core
{
    /// <summary>
    /// Core-level contract for power trade audio cues.
    /// </summary>
    public interface IPowerTradeAudioSource
    {
        AudioClip PowerTradeSfx { get; }

        event Action OnTradeCompleted;
    }
}