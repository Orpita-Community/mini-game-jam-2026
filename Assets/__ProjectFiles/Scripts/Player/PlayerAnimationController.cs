using UnityEngine;
using Orpaits.Player;

namespace Orpaits.Visuals
{
    /// <summary>
    /// Decoupled script that listens to PlayerController events and drives
    /// 2D SpriteRenderer flipping and Animator parameters.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;

        [Header("Respawn")]
        [SerializeField]
        [Tooltip("State forced on respawn. Player_Dead has no outgoing transitions, " +
                 "so the animator must be snapped out of it explicitly.")]
        private string idleStateName = "Player_Idle";
        
        // --- ANIMATOR PARAMETERS ---
        // We will define these exact parameters in the Editor later
        private const string IS_RUNNING = "isRunning";
        private const string Y_VELOCITY = "yVelocity";
        private const string IS_GROUNDED = "isGrounded";
        private const string IS_SKIDDING = "isSkidding";
        private const string JUMP_TRIGGER = "jumpTrigger";
        private const string DIE_TRIGGER = "dieTrigger";

        private bool isCurrentlySkidding = false;
        private SpriteRenderer spriteRenderer;
        private Animator animator;
        private Rigidbody2D playerRb;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            
            // We use the Rigidbody on the player to get the Y velocity for falling animations
            if (playerController != null)
            {
                playerRb = playerController.GetComponent<Rigidbody2D>();
            }
        }

        private void OnEnable()
        {
            if (playerController == null) return;

            // Subscribe to the Observer events you built previously
            playerController.OnMove += HandleMovementVisuals;
            playerController.OnJump += HandleJump;
            playerController.OnLand += HandleLand;
            playerController.OnSkidChanged += HandleSkid;
            playerController.OnDeath += HandleDeath;
            playerController.OnRespawn += HandleRespawn;
        }

        private void OnDisable()
        {
            if (playerController == null) return;

            // Unsubscribe to prevent memory leaks
            playerController.OnMove -= HandleMovementVisuals;
            playerController.OnJump -= HandleJump;
            playerController.OnLand -= HandleLand;
            playerController.OnSkidChanged -= HandleSkid;
            playerController.OnDeath -= HandleDeath;
            playerController.OnRespawn -= HandleRespawn;
        }

        private void Update()
        {
            // The events are great for state changes, but standard animation loops
            // often require checking "continuous" data (like falling speed) in Update.
            if (playerController != null && !playerController.IsDead && playerRb != null)
            {
                // Set a float so the Animator can distinguish between jumping up vs. falling down
                animator.SetFloat(Y_VELOCITY, playerRb.linearVelocity.y);
                animator.SetBool(IS_GROUNDED, playerController.isGrounded);
            }
        }

        // --- OBSERVER EVENT HANDLERS ---

        private void HandleMovementVisuals(float directionInput)
        {
            bool isMoving = Mathf.Abs(directionInput) > 0.01f;
            animator.SetBool(IS_RUNNING, isMoving);
            
            // CRITICAL FIX: Only flip the sprite if we are NOT actively skidding.
            // This locks the sprite facing forward while they dig their heels in to brake!
            if (isMoving && !isCurrentlySkidding)
            {
                spriteRenderer.flipX = (directionInput < -0.01f);
            }
        }

        private void HandleJump()
        {
            animator.SetTrigger(JUMP_TRIGGER);
        }

        private void HandleLand()
        {
            // Re-syncing running state just in case player was holding keys when landing
            // If input is zero, the next OnMove call will be false.
            animator.SetBool(IS_GROUNDED, true);
        }
        //added

        private void HandleSkid(bool isSkidding)
        {
            // Save the state locally so HandleMovementVisuals can read it
            isCurrentlySkidding = isSkidding;
            animator.SetBool(IS_SKIDDING, isSkidding);
        }

        private void HandleDeath()
        {
            animator.SetTrigger(DIE_TRIGGER);

            // Ensure no other states override death
            animator.SetBool(IS_RUNNING, false);
            animator.SetBool(IS_GROUNDED, true);
        }

        private void HandleRespawn()
        {
            animator.ResetTrigger(DIE_TRIGGER);
            animator.ResetTrigger(JUMP_TRIGGER);
            animator.SetBool(IS_RUNNING, false);
            animator.SetBool(IS_SKIDDING, false);
            animator.SetBool(IS_GROUNDED, true);
            animator.SetFloat(Y_VELOCITY, 0f);
            isCurrentlySkidding = false;

            // Player_Dead is a dead-end state: clearing dieTrigger cannot leave it,
            // so snap straight back to Idle.
            animator.Play(idleStateName, 0, 0f);
        }
    }
}