using UnityEngine;

namespace Orpaits.Environment
{
    /// <summary>
    /// A trigger zone that instantly kills any IDamageable that touches it.
    /// Attach this to a child object of the main camera.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class CameraKillZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<Core.IDamageable>(out var damageable) && !damageable.IsDead)
            {
                // Instantly apply maximum damage to trigger the death sequence
                damageable.TakeDamage(damageable.MaxHealth);
            }
        }
    }
}