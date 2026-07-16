using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Orpaits.Core;

namespace Orpaits.Player
{
    /// <summary>
    /// Core Player Controller handling movement, advanced jumping, and health.
    /// Utilizes the Observer pattern via C# events to decouple from UI, Audio, and Animation.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public class PlayerController : MonoBehaviour, IDamageable
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 8f;
        
        [Header("Jump Heights & Gravity")]
        [SerializeField] private float jumpForce = 16f;
        [SerializeField] [Tooltip("Multiplier applied when falling to make drops feel heavier")]
        private float fallMultiplier = 2.5f;
        [SerializeField] [Tooltip("Multiplier applied if player releases the jump button early")]
        private float lowJumpMultiplier = 2f;
        [SerializeField] [Tooltip("Maximum falling speed to prevent phasing through floors")]
        private float terminalVelocity = 25f;

        [Header("Movement Momentum")]
        [SerializeField] [Tooltip("How fast the player reaches max speed")] 
        private float acceleration = 40f;
        [SerializeField] [Tooltip("How fast the player slides to a stop")] 
        private float deceleration = 50f;
        [SerializeField] [Tooltip("Minimum speed required to trigger the skid animation")] 
        private float skidThreshold = 3f;

        [Header("Jump Forgiveness")]
        [SerializeField] [Tooltip("Grace period (seconds) to jump after falling off a ledge.")]
        private float coyoteTime = 0.15f;
        private float coyoteTimeCounter;

        [SerializeField] [Tooltip("Grace period (seconds) to queue a jump before hitting the ground.")]
        private float jumpBufferTime = 0.1f;
        private float jumpBufferCounter;

        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 3f;
        
        [Header("Ground Detection")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.2f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Combat (Zone 3)")]
        [SerializeField] private GameObjectPool dataDiscPool;
        [SerializeField] private Transform throwSpawnPoint;
        public int CurrentAmmo { get; private set; }

        [Header("Input Actions")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference throwAction;

        // --- OBSERVER EVENTS ---
        public event Action<float> OnMove;
        public event Action OnJump;
        public event Action OnLand;
        public event Action OnThrow;
        public event Action<float, float> OnHealthChanged;
        public event Action<float> OnDamageTaken;
        public event Action OnDeath;
        public event Action OnRespawn;
        public event Action<bool> OnSkidChanged;
        private bool isSkidding;

        // State & Component References
        private Rigidbody2D rb;
        public bool isGrounded { get; private set; }
        private bool wasGrounded;
        private float movementInput;

        // IDamageable Properties
        public float CurrentHealth { get; private set; }
        public float MaxHealth => maxHealth;
        public bool IsDead { get; private set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            CurrentHealth = maxHealth;
            // Read the ammo the NPC gave us (defaults to 0 if no trade happened)
            CurrentAmmo = PlayerPrefs.GetInt("Player_DataDiscs", 0);
        }

        [ContextMenu("🎮 Debug: Give 99 Discs")]
        private void DebugGiveAmmo()
        {
            // Give ourselves massive ammo for testing without messing with PlayerPrefs
            CurrentAmmo = 99;
            Debug.Log("[PlayerController] 🎮 Debug: Granted 99 Data Discs!");
        }

        private void OnEnable()
        {
            if (moveAction != null) moveAction.action.Enable();
            
            if (jumpAction != null)
            {
                jumpAction.action.Enable();
                jumpAction.action.performed += HandleJumpInput;
            }
            
            if (throwAction != null)
            {
                throwAction.action.Enable();
                throwAction.action.performed += HandleThrow;
            }
        }

        private void OnDisable()
        {
            if (moveAction != null) moveAction.action.Disable();
            
            if (jumpAction != null)
            {
                jumpAction.action.performed -= HandleJumpInput;
                jumpAction.action.Disable();
            }
            
            if (throwAction != null)
            {
                throwAction.action.performed -= HandleThrow;
                throwAction.action.Disable();
            }
        }

        private void Update()
        {
            if (IsDead) return;

            ReadInput();
            UpdateTimers();
            CheckGroundedAndCoyote();
            
            // 1. Evaluate if we are physically skidding BEFORE broadcasting movement
            CheckSkidStatus(); 
            
            // 2. NOW broadcast the movement to the Animator
            OnMove?.Invoke(movementInput);

            ExecuteJump();
            HandleJumpPhysics();
        }

        private void FixedUpdate()
        {
            if (IsDead) return;

            ApplyMovement();
        }

        // --- INPUT & MOVEMENT ---

        private void ReadInput()
        {
            if (moveAction != null)
            {
                movementInput = moveAction.action.ReadValue<Vector2>().x;
            }
        }

        private void ApplyMovement()
        {
            float targetSpeed = movementInput * moveSpeed;
            float currentSpeed = rb.linearVelocity.x;

            float accelRate;
            if (Mathf.Abs(targetSpeed) > 0.01f)
            {
                if (Mathf.Sign(movementInput) != Mathf.Sign(currentSpeed) && Mathf.Abs(currentSpeed) > 0.1f)
                {
                    accelRate = deceleration * 1.5f; 
                }
                else
                {
                    accelRate = acceleration;
                }
            }
            else
            {
                accelRate = deceleration;
            }

            float newX = Mathf.MoveTowards(currentSpeed, targetSpeed, accelRate * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
            
            // (Notice that the old CheckSkid call has been completely removed from here)
        }

        private void CheckSkidStatus()
        {
            float currentSpeed = rb.linearVelocity.x;
            
            bool isInputZero = Mathf.Abs(movementInput) < 0.1f;
            
            // Check against a much lower threshold (0.5f instead of 3f) so the skid 
            // animation stays active until the player comes to an almost complete stop.
            bool isMovingFast = Mathf.Abs(currentSpeed) > 0.5f;
            
            // Condition 1: Letting go of the keys while running
            bool isHardStopping = isInputZero && isMovingFast;
            
            // Condition 2: Pressing the opposite direction while running
            bool isHardTurning = !isInputZero && Mathf.Sign(movementInput) != Mathf.Sign(currentSpeed) && isMovingFast;

            bool shouldSkid = isGrounded && (isHardStopping || isHardTurning);

            if (shouldSkid != isSkidding)
            {
                isSkidding = shouldSkid;
                OnSkidChanged?.Invoke(isSkidding);
            }
        }

        private void CheckGroundedAndCoyote()
        {
            wasGrounded = isGrounded;
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

            if (isGrounded && !wasGrounded)
            {
                OnLand?.Invoke();
            }

            if (isGrounded)
            {
                coyoteTimeCounter = coyoteTime;
            }
        }

        private void UpdateTimers()
        {
            coyoteTimeCounter -= Time.deltaTime;
            jumpBufferCounter -= Time.deltaTime;
        }

        // --- ACTIONS ---

        private void HandleJumpInput(InputAction.CallbackContext context)
        {
            // Instead of jumping instantly, we "buffer" the input intent
            jumpBufferCounter = jumpBufferTime;
        }

        private void ExecuteJump()
        {
            // If the player recently pressed jump AND recently touched the ground (or is on it)
            if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f && !IsDead)
            {
                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;

                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                
                OnJump?.Invoke();
            }
        }

        private void HandleJumpPhysics()
        {
            // Faster falling
            if (rb.linearVelocity.y < 0)
            {
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
                
                // Terminal velocity clamp
                if (rb.linearVelocity.y < -terminalVelocity)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, -terminalVelocity);
                }
            }
            // Variable jump height (letting go of the button early)
            else if (rb.linearVelocity.y > 0 && jumpAction != null && !jumpAction.action.IsPressed())
            {
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
            }
        }

        private void HandleThrow(InputAction.CallbackContext context)
        {
            // Only throw if alive, we have ammo, and the pool is assigned!
            if (!IsDead && CurrentAmmo > 0 && dataDiscPool != null)
            {
                CurrentAmmo--; // Consume one CD
                
                // Spawn it
                GameObject disc = dataDiscPool.Get(throwSpawnPoint.position, Quaternion.identity);
                
                if (disc != null && disc.TryGetComponent<DataDiscProjectile>(out var p))
                {
                    p.AssignPool(dataDiscPool);
                }

                OnThrow?.Invoke();
                
                Debug.Log($"Threw CD! Ammo remaining: {CurrentAmmo}");
            }
        }

        // --- IDAMAGEABLE IMPLEMENTATION ---

        public bool TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return false;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            
            OnDamageTaken?.Invoke(amount);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (CurrentHealth <= 0f) Die();

            return true;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Check if we hit an enemy
            if (collision.gameObject.CompareTag("Enemy"))
            {
                // Bounce the player upward
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * 10f, ForceMode2D.Impulse);
            }
        }

        public bool Heal(float amount)
        {
            if (IsDead || amount <= 0f || CurrentHealth >= maxHealth) return false;

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            
            return true;
        }

        private void Die()
        {
            if (IsDead) return;
            IsDead = true;
            
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false; 
            
            OnDeath?.Invoke();
        }

        /// <summary>
        /// Resets the player to a specific position for checkpoint respawn.
        /// Revives the player (if dead), restores full health, and clears all
        /// movement/jump/skid state so respawn starts clean. Collected icons are
        /// preserved elsewhere (IconCollectionManager) and untouched here.
        ///
        /// Called by <see cref="Orpaits.Environment.CheckpointManager"/>.
        /// </summary>
        public void ResetToPosition(Vector2 position)
        {
            // Revive: Die() sets IsDead and disables physics simulation.
            IsDead = false;
            rb.simulated = true;

            // Snap to respawn point and kill all momentum.
            transform.position = position;
            rb.linearVelocity = Vector2.zero;
            movementInput = 0f;

            // Clear jump forgiveness so we can't insta-jump on spawn.
            coyoteTimeCounter = 0f;
            jumpBufferCounter = 0f;
            wasGrounded = false;
            isGrounded = false;

            // Drop skid state and notify listeners so the animator resets.
            if (isSkidding)
            {
                isSkidding = false;
                OnSkidChanged?.Invoke(false);
            }

            // Restore health and broadcast so HUD + animation recover.
            CurrentHealth = maxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            OnRespawn?.Invoke();
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }
        }
    }
}