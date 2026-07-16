using System;
using UnityEngine;

namespace Orpaits.Core
{
    /// <summary>
    /// Core-level contract for checkpoint activation audio cues.
    /// </summary>
    public interface ICheckpointAudioSource
    {
        AudioClip CheckpointSfx { get; }

        event Action OnCheckpointActivated;
    }
}