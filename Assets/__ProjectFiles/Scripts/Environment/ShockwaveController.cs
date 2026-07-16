using System.Threading;
using UnityEngine;
using Orpaits.Core;
using Orpaits.Enemies;

namespace Orpaits.Environment
{
    /// <summary>
    /// Handles the Phase 3 Glitch Wave attack.
    ///
    /// When BossVirus.OnShockwave fires, this controller spawns a shockwave
    /// that sweeps across the arena floor. The wave is a moving trigger
    /// collider that damages the player on contact unless they are:
    ///   - Airborne (jumping over the wave), or
    ///   - Shielded (collected 25+ icons and traded for extra shield).
    ///
    /// The shockwave is built procedurally — no prefab needed. A simple
    /// colored quad with an EdgeCollider2D travels from the boss toward
    /// the far wall.
    ///
    /// Design reference: level-design-260712_2153.md (Phase 3 — Glitch Wave),
    /// mechanics-260712_2137.md (Shockwave Attack).
    /// </summary>
    public class ShockwaveController : MonoBehaviour
    {
        [Header("Boss Reference (auto-found if empty)")]
        [SerializeField]
        private BossVirus boss;

        [Header("Wave Geometry")]
        [SerializeField]
        [Tooltip("How fast the wave travels (world units/second).")]
        private float waveSpeed = 6f;

        [SerializeField]
        [Tooltip("Height of the wave (world units). Player must jump this high to clear.")]
        private float waveHeight = 1.5f;

        [SerializeField]
        [Tooltip("Thickness of the wave (world units).")]
        private float waveThickness = 0.5f;

        [Header("Arena Bounds")]
        [SerializeField]
        [Tooltip("How far the wave travels before despawning (world units from boss).")]
        private float waveTravelDistance = 20f;

        [SerializeField]
        [Tooltip("Y position of the arena floor (wave spawns at this height).")]
        private float floorY = -2.5f;

        [Header("Damage")]
        [SerializeField]
        [Tooltip("Damage dealt if the wave hits an unshielded, grounded player.")]
        private float waveDamage = 2f;

        [SerializeField]
        [Tooltip("Layer mask for objects that block the wave (walls, etc.).")]
        private LayerMask blockLayers;

        [Header("Visual")]
        [SerializeField]
        [Tooltip("Color of the shockwave visual.")]
        private Color waveColor = new Color(0.8f, 0.1f, 1f, 0.7f);

        [Header("Shield Check")]
        [SerializeField]
        [Tooltip("Player tag to check for shield component.")]
        private string playerTag = "Player";

        private void OnEnable()
        {
            if (boss == null) boss = FindFirstObjectByType<BossVirus>();
            if (boss == null)
            {
                Debug.LogWarning("[ShockwaveController] No BossVirus found in scene.", this);
                return;
            }

            boss.OnShockwave += HandleShockwave;
        }

        private void OnDisable()
        {
            if (boss == null) return;
            boss.OnShockwave -= HandleShockwave;
        }

        private void HandleShockwave()
        {
            // Determine wave direction: sweep from boss toward arena center
            Vector3 bossPos = boss != null ? boss.transform.position : transform.position;

            // Wave always sweeps in +X direction (toward the player side)
            SpawnWave(bossPos);
        }

        private void SpawnWave(Vector3 origin)
        {
            // Create the wave GameObject
            GameObject wave = new GameObject("BossShockwave");
            wave.transform.SetPositionAndRotation(
                new Vector3(origin.x, floorY + waveHeight * 0.5f, 0f),
                Quaternion.identity);

            // ── Visual: a simple colored sprite ──────────────────────────
            var sr = wave.AddComponent<SpriteRenderer>();
            sr.color = waveColor;
            sr.sortingOrder = 5;

            // Create a 1x1 white texture and scale it
            sr.sprite = CreateWhiteSprite();
            wave.transform.localScale = new Vector3(waveThickness, waveHeight, 1f);

            // ── Collider: trigger to detect player contact ───────────────
            var col = wave.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 1f); // matches the scaled sprite

            // ── Behavior: sweep + damage + despawn ───────────────────────
            var mover = wave.AddComponent<ShockwaveMover>();
            mover.Initialize(
                direction: Vector2.right,
                speed: waveSpeed,
                maxDistance: waveTravelDistance,
                damage: waveDamage,
                playerTag: playerTag,
                blockLayers: blockLayers);

            Debug.Log("[ShockwaveController] Shockwave spawned — JUMP or SHIELD!");
        }

        /// <summary>
        /// Creates a simple 1x1 white sprite at runtime.
        /// Used for the shockwave visual without needing an external texture.
        /// </summary>
        private static Sprite CreateWhiteSprite()
        {
            Texture2D tex = new Texture2D(4, 4);
            Color[] pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }
    }

    /// <summary>
    /// Internal component that drives a single shockwave instance.
    /// Moves the wave, checks for collisions, and despawns when done.
    /// </summary>
    internal class ShockwaveMover : MonoBehaviour
    {
        private Vector2 direction;
        private float speed;
        private float maxDistance;
        private float damage;
        private string playerTag;
        private LayerMask blockLayers;
        private Vector2 startPos;
        private bool hitPlayer;

        public void Initialize(Vector2 direction, float speed, float maxDistance,
                               float damage, string playerTag, LayerMask blockLayers)
        {
            this.direction = direction.normalized;
            this.speed = speed;
            this.maxDistance = maxDistance;
            this.damage = damage;
            this.playerTag = playerTag;
            this.blockLayers = blockLayers;
            startPos = transform.position;
        }

        private void Update()
        {
            // Move
            transform.position += (Vector3)(direction * speed * Time.deltaTime);

            // Despawn after traveling max distance
            float traveled = Vector2.Distance(startPos, transform.position);
            if (traveled >= maxDistance)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hitPlayer) return;

            // Check if blocked by a wall
            if ((blockLayers & (1 << other.gameObject.layer)) != 0)
            {
                Destroy(gameObject);
                return;
            }

            // Check if we hit the player
            if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag))
            {
                hitPlayer = true;

                // Check if player has a shield (can tank the wave)
                if (HasShield(other.gameObject))
                {
                    Debug.Log("[ShockwaveMover] Player tanked the wave with shield!");
                    Destroy(gameObject);
                    return;
                }

                // Check if player is airborne (jumped over the wave)
                if (IsAirborne(other.gameObject))
                {
                    Debug.Log("[ShockwaveMover] Player jumped over the wave!");
                    return; // Don't destroy — player cleared it
                }

                // Player got hit
                if (other.TryGetComponent<IDamageable>(out var damageable) && !damageable.IsDead)
                {
                    damageable.TakeDamage(damage);
                    Debug.Log($"[ShockwaveMover] Wave hit player for {damage} damage!");
                }

                Destroy(gameObject);
            }
        }

        private bool HasShield(GameObject player)
        {
            // Check for any shield-related component or interface
            // The shield system (#18) may use a component or player prefs
            // We check both approaches
            var shieldComponents = player.GetComponents<MonoBehaviour>();
            foreach (var comp in shieldComponents)
            {
                // Check by type name to avoid hard dependency
                if (comp.GetType().Name.Contains("Shield"))
                    return comp.enabled;
            }

            // Also check PlayerPrefs (Anti-Virus trade key)
            return PlayerPrefs.GetInt("Antivirus_Trade_Completed", 0) == 1
                   && PlayerPrefs.GetInt("TotalIconsCollected", 0) >= 25;
        }

        private bool IsAirborne(GameObject player)
        {
            // Player is airborne if their feet are above the wave's top edge
            float playerY = player.transform.position.y;
            float waveTop = transform.position.y + transform.localScale.y * 0.5f;

            // Small tolerance so a well-timed jump clears it
            return playerY > waveTop - 0.3f;
        }
    }
}
