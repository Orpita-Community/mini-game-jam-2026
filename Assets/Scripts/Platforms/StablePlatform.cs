using UnityEngine;

namespace Orpaits.Platforms
{
    public class StablePlatform : BasePlatform
    {
        [Header("Stable Platform")]
        [SerializeField, Tooltip("Visual variant identifier for level design")]
        private string variant = "Default";
    }
}
