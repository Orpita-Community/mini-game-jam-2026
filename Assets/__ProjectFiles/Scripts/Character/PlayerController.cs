using UnityEngine;
using UnityEngine.InputSystem;

namespace Orpaits.Character
{
    /// <summary>
    /// Core player movement controller for Orpaits.
    /// Handles horizontal movement, jumping, and collision detection
    /// using Rigidbody2D physics-based movement.
    ///
    /// Design reference: mechanics-260712_2137.md (Player Movement)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField]
        [Tooltip("Top horizontal speed (units/sec)")]
        private float moveSpeed = 8f;

        [SerializeField]
        [Tooltip("Time to reach full speed (seconds)")]
        private float accelerationTime = 0.15f;

        [SerializeField]
        [Tooltip("Time to stop from full speed (seconds)")]
        private float decelerationTime = 0.1f;

        [Header("Jump")]
        [SerializeField]
        [Tooltip("Jump launch velocity (upward)")]
        private float jumpForce = 14f;

        [SerializeField]
        [Tooltip("Gravity multiplier during jump ascent")]
        private float jumpUpGravity = 1f;

        [SerializeField]
        [Tooltip("Gravity multiplier during jump descent")]
        private float jumpDownGravity = 2.5f;

        [SerializeField]
        [Tooltip("Coyote time: time after leaving ground that jump is still allowed (seconds)")]
        private float coyoteTime = 0.1f;

        [SerializeField]
        [Tooltip("Jump buffer: time before landing that jump input is buffered (seconds)")]
        private float jumpBufferTime = 0.1f;

        [Header("Ground Check")]
        [SerializeField]
        [Tooltip("Transform for ground check position (defaults to center-bottom)")]
        private Transform groundCheckPoint;

        [SerializeField]
        [Tooltip("Radius of ground check sphere")]
        private float groundCheckRadius = 0.1f;

        [SerializeField]
        [Tooltip("Layers considered ground")]
        private LayerMask groundLayer = 1; // Default layer

        // Components
        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private PlayerInput playerInput;

        // Input state
        private Vector2 moveInput;
        private bool jumpPressed;
        private bool jumpHeld;

        // Movement state
        private float currentHorizontalVelocity;
        private bool isGrounded;
        private float lastGroundedTime;
        private float lastJumpPressTime;
        private bool hasBufferedJump;

        // Properties
        public bool IsGrounded => isGrounded;
        public bool IsMoving => Mathf.Abs(rb.linearVelocityX) > 0.1f;
        public bool IsJumping => rb.linearVelocityY > 0.1f && !isGrounded;
        public bool IsFalling => rb.linearVelocityY < -0.1f && !isGrounded;
        public Vector2 Velocity => rb.linearVelocity;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            playerInput = GetComponent<PlayerInput>();

            if (groundCheckPoint == null)
            {
                // Default to center-bottom of collider bounds
                var collider = GetComponent<Collider2D>();
                var groundCheckObj = new GameObject("GroundCheck");
                groundCheckObj.transform.SetParent(transform);
                groundCheckObj.transform.localPosition = new Vector3(0, -collider.bounds.extents.y, 0);
                groundCheckPoint = groundCheckObj.transform;
            }
        }

        private void OnEnable()
        {
            if (playerInput != null)
                playerInput.onActionTriggered += OnActionTriggered;
        }

        private void OnDisable()
        {
            if (playerInput != null)
                playerInput.onActionTriggered -= OnActionTriggered;
        }

        private void OnActionTriggered(InputAction.CallbackContext context)
        {
            switch (context.action.name)
            {
                case "Move":
                    OnMove(context);
                    break;
                case "Jump":
                    OnJump(context);
                    break;
            }
        }

        private void Update()
        {
            // Ground check
            isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);
            if (isGrounded)
                lastGroundedTime = Time.time;

            // Jump buffer
            if (jumpPressed)
            {
                lastJumpPressTime = Time.time;
                jumpPressed = false;
                hasBufferedJump = true;
            }

            // Check for buffered jump
            bool canCoyoteJump = Time.time - lastGroundedTime < coyoteTime;
            bool hasBuffered = Time.time - lastJumpPressTime < jumpBufferTime;

            if (hasBufferedJump && hasBuffered && canCoyoteJump && rb.linearVelocityY <= 0)
            {
                PerformJump();
                hasBufferedJump = false;
                lastGroundedTime = float.MinValue; // Prevent re-trigger
            }

            // Flip sprite based on movement direction
            if (moveInput.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (moveInput.x < -0.01f)
                spriteRenderer.flipX = true;
        }

        private void FixedUpdate()
        {
            // Horizontal movement with acceleration/deceleration
            float targetVelocity = moveInput.x * moveSpeed;

            if (Mathf.Abs(moveInput.x) > 0.01f)
            {
                // Accelerate
                float accel = moveSpeed / accelerationTime;
                currentHorizontalVelocity = Mathf.MoveTowards(
                    rb.linearVelocityX,
                    targetVelocity,
                    accel * Time.fixedDeltaTime
                );
            }
            else
            {
                // Decelerate
                float decel = moveSpeed / decelerationTime;
                currentHorizontalVelocity = Mathf.MoveTowards(
                    rb.linearVelocityX,
                    0f,
                    decel * Time.fixedDeltaTime
                );
            }

            // Apply horizontal velocity (preserve vertical)
            rb.linearVelocity = new Vector2(currentHorizontalVelocity, rb.linearVelocityY);

            // Apply variable jump gravity
            if (rb.linearVelocityY < 0)
            {
                // Falling - increase gravity for snappier fall
                rb.gravityScale = jumpDownGravity;
            }
            else if (rb.linearVelocityY > 0 && !jumpHeld)
            {
                // Rising but released jump - shorter jump
                rb.gravityScale = jumpDownGravity * 1.5f;
            }
            else
            {
                // Rising and holding jump - normal gravity
                rb.gravityScale = jumpUpGravity;
            }
        }

        private void PerformJump()
        {
            rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpForce);
            rb.gravityScale = jumpUpGravity;
        }

        // --- Input System callbacks ---

        public void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                jumpPressed = true;
                jumpHeld = true;
            }
            else if (context.canceled)
            {
                jumpHeld = false;
            }
        }

        /// <summary>
        /// Resets the player to a specific position (for checkpoint respawn).
        /// </summary>
        public void ResetToPosition(Vector2 position)
        {
            rb.linearVelocity = Vector2.zero;
            transform.position = position;
            currentHorizontalVelocity = 0f;
            isGrounded = false;
            hasBufferedJump = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheckPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
            }
        }
    }
}
