using UnityEngine;
using Orpaits.Enemies;

namespace Orpaits.Environment
{
    /// <summary>
    /// Boss arena entrance: an invisible trigger that activates when the player
    /// crosses into the arena. Closes the entrance door behind them and starts
    /// the boss fight via <see cref="BossVirus.StartBossFight"/>.
    ///
    /// Designed so the player cannot leave the arena once the fight starts.
    /// The door is a separate GameObject with its own collider; this script
    /// only toggles its active state.
    ///
    /// Design reference: level-design-260712_2153.md (Boss Fight Details),
    /// game-loop-260712_2137.md (Combat Phase).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BossArenaEntrance : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        [Tooltip("The door GameObject to activate when the player enters.")]
        private GameObject entranceDoor;

        [SerializeField]
        [Tooltip("The BossVirus in the arena. StartBossFight() will be called.")]
        private BossVirus bossVirus;

        [Header("Trigger")]
        [SerializeField]
        [Tooltip("Tag that activates the trigger. Usually 'Player'.")]
        private string targetTag = "Player";

        /// <summary>Has this entrance already been activated?</summary>
        public bool IsActivated { get; private set; }

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsActivated) return;
            if (string.IsNullOrEmpty(targetTag) || !other.CompareTag(targetTag)) return;

            Activate();
        }

        /// <summary>
        /// Closes the door and starts the boss fight. Public so it can be
        /// wired to other triggers (e.g. an explicit "Start Fight" button
        /// after a cutscene) if needed.
        /// </summary>
        public void Activate()
        {
            if (IsActivated) return;
            IsActivated = true;

            if (entranceDoor != null)
                entranceDoor.SetActive(true);
            else
                Debug.LogWarning("[BossArenaEntrance] No entranceDoor assigned — arena cannot be sealed!", this);

            if (bossVirus != null)
                bossVirus.StartBossFight();
            else
                Debug.LogWarning("[BossArenaEntrance] No bossVirus assigned — fight cannot start!", this);

            Debug.Log("[BossArenaEntrance] Arena sealed. Boss fight starting...");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.6f); // orange
            var col = GetComponent<Collider2D>();
            if (col != null)
                Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }
    }
}
