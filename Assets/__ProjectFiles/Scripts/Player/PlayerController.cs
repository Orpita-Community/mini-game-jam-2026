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
        public event Action<bool> OnSkidChanged;
        private bool isSkidding;

        // State & Component References
        private Rigidbody2D rb;
        private bool isGrounded;
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
                
                if (Mathf.Abs(movementInput) > 0.01f)
                {
                    OnMove?.Invoke(movementInput);
                }
            }
        }

        private void ApplyMovement()
        {
            float targetSpeed = movementInput * moveSpeed;
            float currentSpeed = rb.linearVelocity.x;

            // Determine if we are speeding up, slowing down, or turning around
            float accelRate;
            
            if (Mathf.Abs(targetSpeed) > 0.01f)
            {
                // We are holding a movement key
                if (Mathf.Sign(movementInput) != Mathf.Sign(currentSpeed) && Mathf.Abs(currentSpeed) > 0.1f)
                {
                    // We are holding the OPPOSITE direction of our movement (Hard turn/brake)
                    accelRate = deceleration * 1.5f; 
                }
                else
                {
                    // Standard acceleration
                    accelRate = acceleration;
                }
            }
            else
            {
                // No input, slowing down to a halt
                accelRate = deceleration;
            }

            // Apply the momentum
            float newX = Mathf.MoveTowards(currentSpeed, targetSpeed, accelRate * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);

            // --- SKID DETECTION FOR ANIMATOR ---
            CheckSkid(currentSpeed);
        }

        private void CheckSkid(float currentSpeed)
        {
            // A skid happens if we are on the ground and moving fast enough, AND:
            // 1. We let go of the keys (trying to stop) OR
            // 2. We are pressing the opposite direction (trying to turn)
            
            bool isTryingToStop = movementInput == 0 && Mathf.Abs(currentSpeed) > skidThreshold;
            bool isTryingToTurn = movementInput != 0 && Mathf.Sign(movementInput) != Mathf.Sign(currentSpeed) && Mathf.Abs(currentSpeed) > skidThreshold;

            bool shouldSkid = isGrounded && (isTryingToStop || isTryingToTurn);

            // Only fire the event when the state actually changes
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
            if (!IsDead)
            {
                OnThrow?.Invoke();
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