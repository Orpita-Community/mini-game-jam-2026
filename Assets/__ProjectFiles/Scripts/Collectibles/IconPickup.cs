    using UnityEngine;

namespace Orpaits.Collectibles
{
    /// <summary>
    /// A physical nostalgic icon in the world. The player walks into the
    /// trigger collider to collect it; the icon is then added to the
    /// <see cref="IconCollectionManager"/> inventory.
    ///
    /// Collection is permanent: once collected, the icon stays in the
    /// inventory across death/respawn (see CheckpointManager). The pickup
    /// GameObject deactivates itself after collection so it cannot be
    /// re-collected.
    ///
    /// Design reference: mechanics-260712_2137.md (Collectible System §2).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class IconPickup : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField]
        [Tooltip("The icon variant this pickup represents. Required.")]
        private IconData data;

        [Header("Presentation")]
        [SerializeField]
        [Tooltip("Idle rotation speed (degrees/sec). 0 = no spin.")]
        private float spinSpeed = 60f;

        [SerializeField]
        [Tooltip("Idle vertical bob amplitude (world units).")]
        private float bobAmplitude = 0.1f;

        [SerializeField]
        [Tooltip("Idle vertical bob frequency (Hz).")]
        private float bobFrequency = 1.5f;

        [Header("Pickup VFX (optional)")]
        [SerializeField]
        [Tooltip("Particle system played on pickup. Optional.")]
        private ParticleSystem pickupVFX;

        [SerializeField]
        [Tooltip("Audio clip played on pickup. Optional.")]
        private AudioClip pickupSFX;

        [Header("Trigger")]
        [SerializeField]
        [Tooltip("Tag that triggers collection. Usually 'Player'.")]
        private string targetTag = "Player";

        /// <summary>The icon variant this pickup represents.</summary>
        public IconData Data => data;

        /// <summary>Has this pickup already been collected?</summary>
        public bool IsCollected { get; private set; }

        /// <summary>Fired exactly once when the icon is collected.</summary>
        public event System.Action<IconPickup> OnCollected;

        private SpriteRenderer spriteRenderer;
        private AudioSource audioSource;
        private Vector3 baseLocalPos;
        private float phase;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            audioSource = GetComponent<AudioSource>();

            // Ensure collider is a trigger (matches KillZone pattern)
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;

            baseLocalPos = transform.localPosition;
            phase = Random.Range(0f, Mathf.PI * 2f); // desync bob between icons

            ApplyData();
        }

        private void OnValidate()
        {
            // Keep inspector in sync when data is reassigned
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            ApplyData();
        }

        private void Update()
        {
            if (IsCollected) return;
            

            if (bobAmplitude != 0f)
            {
                phase += bobFrequency * Mathf.PI * 2f * Time.deltaTime;
                float y = Mathf.Sin(phase) * bobAmplitude;
                transform.localPosition = baseLocalPos + new Vector3(0, y, 0);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsCollected) return;
            if (string.IsNullOrEmpty(targetTag) || !other.CompareTag(targetTag)) return;

            Collect();
        }

        /// <summary>
        /// Collects this icon: notifies the <see cref="IconCollectionManager"/>,
        /// plays VFX/SFX, then disables this GameObject so it cannot be
        /// re-collected.
        /// </summary>
        public void Collect()
        {
            if (IsCollected) return;
            if (data == null)
            {
                Debug.LogError($"[IconPickup] '{name}' has no IconData assigned!", this);
                return;
            }

            IsCollected = true;

            var manager = IconCollectionManager.Instance;
            if (manager != null)
                manager.AddIcon(data);
            else
                Debug.LogError($"[IconPickup] '{name}' collected but no IconCollectionManager exists — " +
                               "the icon is lost and the HUD will not update.", this);

            if (pickupVFX != null)
            {
                pickupVFX.transform.SetParent(null, worldPositionStays: true);
                pickupVFX.Play();
                Destroy(pickupVFX.gameObject, pickupVFX.main.duration + 0.5f);
            }

            if (pickupSFX != null && audioSource != null)
                AudioSource.PlayClipAtPoint(pickupSFX, transform.position);

            OnCollected?.Invoke(this);
            Debug.Log($"[IconPickup] Collected '{data.DisplayName}' at {transform.position}");

            gameObject.SetActive(false);
        }

        private void ApplyData()
        {
            if (data == null || spriteRenderer == null) return;
            if (data.Sprite != null)
                spriteRenderer.sprite = data.Sprite;
            spriteRenderer.color = data.Tint;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0f); // golden
            var col = GetComponent<Collider2D>();
            if (col != null)
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
