using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings — defaults, overridden by PlayerStats at runtime")]
    public float moveSpeed = 10f;
    public float acceleration = 20f;
    public float deceleration = 25f;
    public float jumpForce = 12f;
    [Tooltip("Fixed at 1 — player only jumps when grounded. Not upgradeable.")]
    public int maxJumps = 1;

    [Header("Wall Jump Settings")]
    public float wallJumpForce = 10f;
    public Vector2 wallJumpDirection = new Vector2(1f, 1f).normalized;

    [Header("Wall Slide Settings")]
    public float wallSlideStopSpeed = -2f;
    public float wallLerpToZeroTime = 0.5f;
    public float wallSlideDelay = 2f;
    public float wallSlideAccelerationTime = 5f;
    public float maxWallSlideSpeed = -3f;
    public LayerMask wallLayer;
    public float wallCheckDistance = 0.6f;

    [Header("Wall Check — vertical spread")]
    [Tooltip("Number of raycasts stacked vertically for wall detection.")]
    public int wallCheckRayCount = 3;
    [Tooltip("Half-height of the spread. Match to roughly half your collider height.")]
    public float wallCheckHalfHeight = 0.4f;

    [Header("Wall Jump — post-jump lockout")]
    [Tooltip("Seconds after a wall jump during which wall contacts are ignored. " +
             "Prevents immediately re-grabbing the same wall and inflating the jump.")]
    public float wallJumpCooldown = 0.25f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.3f;
    public LayerMask groundLayer;

    [Header("Debug")]
    public Vector3 currentVelocity;
    public float currentYVelocity = 0f;

    private float MoveSpeed => playerStats != null ? playerStats.moveSpeed : moveSpeed;
    private float JumpForce => playerStats != null ? playerStats.jumpForce : jumpForce;
    private int MaxJumps => maxJumps;
    private float MaxWallSlide => playerStats != null ? playerStats.wallSlideSpeed : maxWallSlideSpeed;
    private float Acceleration => playerStats != null ? playerStats.acceleration : acceleration;

    private Rigidbody rb;
    private PlayerStats playerStats;
    private PlayerUpgradeManager upgradeManager;

    private bool isFacingRight = true;
    private int jumpCount;
    public bool isWallSliding;
    public bool isGrounded;
    private float fixedZPosition;

    private bool touchingWallLeft;
    private bool touchingWallRight;

    private float wallLerpTimer;
    private float wallSlideDelayTimer;
    private float wallSlideAccelerationTimer;
    private float wallJumpCooldownTimer = 0f;

    private Vector3 previousPosition;

    public enum WallSlidePhase { None, LerpToZero, WaitingAtZero, AcceleratingToSlide, Sliding }
    public WallSlidePhase wallSlidePhase = WallSlidePhase.None;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        fixedZPosition = transform.position.z;
        rb.useGravity = true;
        playerStats = GetComponent<PlayerStats>();
        upgradeManager = GetComponent<PlayerUpgradeManager>();
    }

    private void Start()
    {
        previousPosition = transform.position;
    }

    public Vector3 GetVelocity() => rb.linearVelocity;
    public void RestoreJumpCharges() => jumpCount = 0;

    private void Update()
    {
        if (!GetComponent<PlayerStateController>().HasControl())
        {
            currentVelocity = rb.linearVelocity;
            return;
        }

        if (wallJumpCooldownTimer > 0f)
            wallJumpCooldownTimer -= Time.deltaTime;

        HandleInput();
        CheckGround();
        CheckWallContacts();
        CheckWallSlideState();
        Flip();

        currentVelocity = rb.linearVelocity;
        currentYVelocity = rb.linearVelocity.y;

        Vector3 delta = transform.position - previousPosition;
        if (delta.sqrMagnitude > 0f)
        {
            bool airborne = !isGrounded && !isWallSliding;
            playerStats?.RecordMovement(delta.x, delta.y, Time.deltaTime, isGrounded, airborne);
        }
        previousPosition = transform.position;

        if (isWallSliding && rb.linearVelocity.y < 0f)
            playerStats?.RecordWallSlideTick(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        var state = GetComponent<PlayerStateController>();

        if (!state.HasControl())
        {
            if (state.AllowKickMovement()) return;
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        ApplyHorizontalMovement();
        ApplyWallSlideBehavior();

        Vector3 pos = rb.position;
        pos.z = fixedZPosition;
        rb.position = pos;
    }

    private void HandleInput()
    {
        if (!GetComponent<PlayerStateController>().HasControl()) return;

        if (Input.GetButtonDown("Jump"))
        {
            if (isWallSliding)
            {
                DoWallJump();
            }
            else if (isGrounded)
            {
                // Zero out any downward velocity before applying jump so the force
                // is always consistent — prevents "stuck in corner" super-jumps
                // caused by accumulated downward velocity being suddenly overridden
                // by a full upward impulse stacked on top
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, 0f);
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, JumpForce, 0f);
                jumpCount = 1;
                ResetWallSlide();
                playerStats?.RecordJump();
                upgradeManager?.Jump(1);
            }
            else if (jumpCount < MaxJumps)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, JumpForce, 0f);
                jumpCount++;
                playerStats?.RecordJump();
                upgradeManager?.Jump(jumpCount);
            }
        }
    }

    private void DoWallJump()
    {
        float dir = touchingWallRight ? -1f : 1f;

        // Fully replace velocity — guarantees identical impulse every time
        // regardless of slide speed or pre-existing horizontal velocity
        rb.linearVelocity = new Vector3(
            wallJumpDirection.x * dir * wallJumpForce,
            wallJumpDirection.y * wallJumpForce,
            0f);

        jumpCount++;
        wallJumpCooldownTimer = wallJumpCooldown;   // suppress wall contact briefly
        ResetWallSlide();
        playerStats?.RecordJump();
        upgradeManager?.Jump(jumpCount);
    }

    private void ApplyHorizontalMovement()
    {
        float input = Input.GetAxisRaw("Horizontal");

        // Only block movement into a wall when NOT in post-wall-jump cooldown.
        // During cooldown the player needs to be able to move away freely.
        if (wallJumpCooldownTimer <= 0f)
        {
            if ((input > 0 && touchingWallRight) || (input < 0 && touchingWallLeft))
                input = 0;
        }

        float targetSpeed = input * MoveSpeed;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Acceleration : deceleration;
        float velocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(velocityX, rb.linearVelocity.y, -2f);
    }

    private void CheckGround()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded) jumpCount = 0;
    }

    /// <summary>
    /// Multi-ray vertical spread — catches edge contacts the single centre ray misses.
    /// Suppressed entirely during wall-jump cooldown to prevent re-grabbing the same wall.
    /// </summary>
    private void CheckWallContacts()
    {
        touchingWallRight = false;
        touchingWallLeft = false;

        if (wallJumpCooldownTimer > 0f) return;

        int count = Mathf.Max(1, wallCheckRayCount);
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : (float)i / (count - 1);
            float yOffset = Mathf.Lerp(-wallCheckHalfHeight, wallCheckHalfHeight, t);
            Vector3 origin = transform.position + Vector3.up * yOffset;

            if (!touchingWallRight && Physics.Raycast(origin, Vector3.right, wallCheckDistance, wallLayer))
                touchingWallRight = true;
            if (!touchingWallLeft && Physics.Raycast(origin, Vector3.left, wallCheckDistance, wallLayer))
                touchingWallLeft = true;

            if (touchingWallRight && touchingWallLeft) break;
        }
    }

    private void CheckWallSlideState()
    {
        if ((touchingWallRight || touchingWallLeft) && !isGrounded)
        {
            if (!isWallSliding)
            {
                isWallSliding = true;
                StartWallSlideSequence();
                playerStats?.RecordWallSlideStart();
                upgradeManager?.WallSlideStart();
            }
        }
        else if (isWallSliding)
        {
            ResetWallSlide();
        }
    }

    private void StartWallSlideSequence()
    {
        wallSlidePhase = WallSlidePhase.LerpToZero;
        wallLerpTimer = 0f;
        wallSlideDelayTimer = 0f;
        wallSlideAccelerationTimer = 0f;
    }

    private void ApplyWallSlideBehavior()
    {
        if (!isWallSliding) return;

        upgradeManager?.WallSlideTick(Time.fixedDeltaTime);

        switch (wallSlidePhase)
        {
            case WallSlidePhase.LerpToZero:
                wallLerpTimer += Time.fixedDeltaTime;
                float lerpT = Mathf.Clamp01(wallLerpTimer / wallLerpToZeroTime);
                float newY = Mathf.Lerp(rb.linearVelocity.y, wallSlideStopSpeed, lerpT);
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, newY, 0f);
                if (lerpT >= 1f) { wallSlidePhase = WallSlidePhase.AcceleratingToSlide; wallSlideDelayTimer = 0f; }
                break;

            case WallSlidePhase.AcceleratingToSlide:
                wallSlideAccelerationTimer += Time.fixedDeltaTime;
                float accelT = Mathf.Clamp01(wallSlideAccelerationTimer / wallSlideAccelerationTime);
                float acceleratingY = Mathf.Lerp(wallSlideStopSpeed, MaxWallSlide, accelT);
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, acceleratingY, 0f);
                if (accelT >= 1f) wallSlidePhase = WallSlidePhase.Sliding;
                break;

            case WallSlidePhase.Sliding:
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, MaxWallSlide, 0f);
                break;
        }
    }

    private void ResetWallSlide()
    {
        isWallSliding = false;
        upgradeManager?.WallSlideEnd();
        wallSlidePhase = WallSlidePhase.None;
        wallLerpTimer = 0f;
        wallSlideDelayTimer = 0f;
        wallSlideAccelerationTimer = 0f;
    }

    private void Flip()
    {
        float input = Input.GetAxisRaw("Horizontal");
        if (input > 0 && !isFacingRight) Turn();
        else if (input < 0 && isFacingRight) Turn();
    }

    private void Turn()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        int count = Mathf.Max(1, wallCheckRayCount);
        Gizmos.color = Color.red;
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : (float)i / (count - 1);
            float yOffset = Mathf.Lerp(-wallCheckHalfHeight, wallCheckHalfHeight, t);
            Vector3 origin = transform.position + Vector3.up * yOffset;
            Gizmos.DrawRay(origin, Vector3.right * wallCheckDistance);
            Gizmos.DrawRay(origin, Vector3.left * wallCheckDistance);
        }
    }
}