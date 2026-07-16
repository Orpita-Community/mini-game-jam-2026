using System.Collections.Generic;
using UnityEngine;
using Orpaits.Core;
using Orpaits.Enemies;
using Orpaits.Platforms;

namespace Orpaits.Environment
{
    /// <summary>
    /// Handles the Phase 2 "The Deletion" mechanic.
    ///
    /// When BossVirus.OnPlatformDeletion fires (with a 0..1 percentage),
    /// this controller randomly selects that fraction of arena platforms
    /// and makes them collapse — shaking briefly then falling away.
    ///
    /// Platforms are found either:
    ///   - By searching children of an assigned arenaRoot transform, or
    ///   - By finding all BasePlatform components in the scene if no root is set.
    ///
    /// Each deletion is permanent for the rest of the fight (platforms don't
    /// respawn during boss battle). This creates increasing pressure as the
    /// safe floor space shrinks.
    ///
    /// Design reference: level-design-260712_2153.md (Phase 2 — The Deletion),
    /// mechanics-260712_2137.md (Platform Deletion).
    /// </summary>
    public class PlatformDeletionHandler : MonoBehaviour
    {
        [Header("Boss Reference (auto-found if empty)")]
        [SerializeField]
        private BossVirus boss;

        [Header("Arena Platforms")]
        [SerializeField]
        [Tooltip("Parent transform containing all deletable arena platforms. " +
                 "If empty, searches the entire scene for BasePlatform components.")]
        private Transform arenaPlatformRoot;

        [SerializeField]
        [Tooltip("Only platforms with this tag are candidates for deletion. " +
                 "Leave empty to consider all platforms.")]
        private string deletableTag = "";

        [Header("Deletion Visual")]
        [SerializeField]
        [Tooltip("How long a platform shakes before it falls (seconds).")]
        private float shakeDuration = 0.8f;

        [SerializeField]
        [Tooltip("How fast a deleted platform falls.")]
        private float fallSpeed = 4f;

        [SerializeField]
        [Tooltip("How far a deleted platform falls before disappearing.")]
        private float fallDistance = 8f;

        [SerializeField]
        [Tooltip("Shake intensity for the deletion warning.")]
        private float shakeIntensity = 0.15f;

        [Header("Safety")]
        [SerializeField]
        [Tooltip("Minimum number of platforms to always keep (prevents deleting everything).")]
        private int minPlatformsToKeep = 2;

        [SerializeField]
        [Tooltip("Never delete a platform the player is currently standing on.")]
        private bool protectPlayerPlatform = true;

        private readonly List<GameObject> allPlatforms = new();
        private readonly HashSet<GameObject> deletedPlatforms = new();

        private void OnEnable()
        {
            if (boss == null) boss = FindFirstObjectByType<BossVirus>();
            if (boss == null)
            {
                Debug.LogWarning("[PlatformDeletionHandler] No BossVirus found in scene.", this);
                return;
            }

            boss.OnPlatformDeletion += HandlePlatformDeletion;
        }

        private void OnDisable()
        {
            if (boss == null) return;
            boss.OnPlatformDeletion -= HandlePlatformDeletion;
        }

        private void Start()
        {
            CachePlatforms();
        }

        /// <summary>
        /// Gather all candidate platform GameObjects at start.
        /// Called once to avoid repeated FindObjectsByType calls.
        /// </summary>
        private void CachePlatforms()
        {
            allPlatforms.Clear();

            if (arenaPlatformRoot != null)
            {
                // Search children of the assigned root
                foreach (Transform child in arenaPlatformRoot)
                {
                    if (IsValidPlatform(child.gameObject))
                        allPlatforms.Add(child.gameObject);
                }
            }
            else
            {
                // Search entire scene
                var platforms = FindObjectsByType<BasePlatform>(FindObjectsSortMode.None);
                foreach (var p in platforms)
                {
                    if (p != null && IsValidPlatform(p.gameObject))
                        allPlatforms.Add(p.gameObject);
                }
            }

            Debug.Log($"[PlatformDeletionHandler] Cached {allPlatforms.Count} arena platforms.");
        }

        private bool IsValidPlatform(GameObject go)
        {
            // Skip already-deleted
            if (deletedPlatforms.Contains(go)) return false;

            // Tag filter
            if (!string.IsNullOrEmpty(deletableTag) && !go.CompareTag(deletableTag))
                return false;

            return true;
        }

        private void HandlePlatformDeletion(float percentage)
        {
            // Refresh cache to exclude already-deleted platforms
            allPlatforms.RemoveAll(p => p == null || deletedPlatforms.Contains(p));

            if (allPlatforms.Count == 0)
            {
                Debug.Log("[PlatformDeletionHandler] No platforms left to delete.");
                return;
            }

            // Calculate how many to delete
            int available = allPlatforms.Count;
            int toKeep = Mathf.Max(minPlatformsToKeep, Mathf.FloorToInt(available * (1f - percentage)));
            int toDelete = Mathf.Max(0, available - toKeep);

            if (toDelete == 0)
            {
                Debug.Log("[PlatformDeletionHandler] Deletion percentage too small to remove any platform.");
                return;
            }

            // Select platforms to delete (random, but protect player platform if enabled)
            List<GameObject> candidates = new(allPlatforms);

            // Shuffle
            Shuffle(candidates);

            int deleted = 0;
            foreach (var platform in candidates)
            {
                if (deleted >= toDelete) break;
                if (platform == null) continue;

                // Skip platform player is standing on
                if (protectPlayerPlatform)
                {
                    var bp = platform.GetComponent<BasePlatform>();
                    if (bp != null && bp.IsPlayerOnPlatform)
                        continue;
                }

                deletedPlatforms.Add(platform);
                _ = DeletePlatformAsync(platform);
                deleted++;
            }

            Debug.Log($"[PlatformDeletionHandler] Deleted {deleted}/{available} platforms " +
                      $"({percentage:P0} requested, {toKeep} kept).");
        }

        /// <summary>
        /// Shake the platform briefly, then make it fall and disappear.
        /// Uses async Awaitable for the timing.
        /// </summary>
        private async Awaitable DeletePlatformAsync(GameObject platform)
        {
            if (platform == null) return;

            Vector3 originalPos = platform.transform.position;
            var collider = platform.GetComponent<Collider2D>();
            var renderer = platform.GetComponent<SpriteRenderer>();

            // ── Phase 1: Shake (warning) ─────────────────────────────
            float timer = 0f;
            float shakeSpeed = 25f;

            while (timer < shakeDuration)
            {
                timer += Time.deltaTime;
                float shakeX = Mathf.Sin(timer * shakeSpeed) * shakeIntensity;
                float shakeY = Mathf.Cos(timer * shakeSpeed * 1.3f) * shakeIntensity * 0.5f;
                if (platform != null)
                    platform.transform.position = originalPos + new Vector3(shakeX, shakeY, 0);
                await Awaitable.NextFrameAsync();
            }

            if (platform == null) return;

            // Reset position
            platform.transform.position = originalPos;

            // ── Phase 2: Disable collider, fall away ─────────────────
            if (collider != null) collider.enabled = false;

            // Tint to red/dark to show corruption
            if (renderer != null)
                renderer.color = new Color(0.6f, 0.15f, 0.15f, 1f);

            Vector3 fallTarget = originalPos + Vector3.down * fallDistance;
            timer = 0f;
            float fallDuration = fallDistance / fallSpeed;

            while (timer < fallDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / fallDuration);

                if (platform != null)
                {
                    platform.transform.position = Vector3.Lerp(originalPos, fallTarget, t);

                    // Fade out as it falls
                    if (renderer != null)
                    {
                        Color c = renderer.color;
                        c.a = 1f - t;
                        renderer.color = c;
                    }
                }

                await Awaitable.NextFrameAsync();
            }

            // ── Phase 3: Deactivate ──────────────────────────────────
            if (platform != null)
                platform.SetActive(false);
        }

        private static void Shuffle(List<GameObject> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
