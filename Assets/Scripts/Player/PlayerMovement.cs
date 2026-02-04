using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float acceleration = 20f;
    public float deceleration = 25f;
    public float jumpForce = 12f;
    public int maxJumps = 2;

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

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.3f;
    public LayerMask groundLayer;

    [Header("Debug")]
    public Vector3 currentVelocity;
    public float currentYVelocity = 0f;

    private Rigidbody rb;
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

    private PlayerUpgradeManager upgradeManager;


    public enum WallSlidePhase { None, LerpToZero, WaitingAtZero, AcceleratingToSlide, Sliding }
    public WallSlidePhase wallSlidePhase = WallSlidePhase.None;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        fixedZPosition = transform.position.z;
        rb.useGravity = true;
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

        HandleInput();
        CheckGround();
        CheckWallContacts();
        CheckWallSlideState();
        Flip();
        currentVelocity = rb.linearVelocity;
        currentYVelocity = rb.linearVelocity.y;

    }

    private void FixedUpdate()
    {
        var state = GetComponent<PlayerStateController>();

        if (!state.HasControl())
        {
            if (state.AllowKickMovement())
                return;

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
        if (!GetComponent<PlayerStateController>().HasControl())
            return;

        if (Input.GetButtonDown("Jump"))
        {
            if (isWallSliding)
            {
                DoWallJump();
            }
            else if (isGrounded)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, 0f);
                jumpCount = 1;
                ResetWallSlide();
            }
        }
    }

    private void DoWallJump()
    {
        float direction = touchingWallRight ? -1f : 1f;
        Vector3 jumpVelocity = new Vector3(wallJumpDirection.x * direction * wallJumpForce, wallJumpDirection.y * wallJumpForce, 0f);
        rb.linearVelocity = jumpVelocity;
        jumpCount++;
        ResetWallSlide();
    }

    private void ApplyHorizontalMovement()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");

        if ((moveInput > 0 && touchingWallRight) || (moveInput < 0 && touchingWallLeft))
        {
            moveInput = 0;
        }

        float targetSpeed = moveInput * moveSpeed;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

        float velocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(velocityX, rb.linearVelocity.y, -2f);
    }

    private void CheckGround()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded) jumpCount = 0;
    }

    private void CheckWallContacts()
    {
        touchingWallRight = Physics.Raycast(transform.position, Vector3.right, wallCheckDistance, wallLayer);
        touchingWallLeft = Physics.Raycast(transform.position, Vector3.left, wallCheckDistance, wallLayer);
    }

    private void CheckWallSlideState()
    {
        if ((touchingWallRight || touchingWallLeft) && !isGrounded)
        {
            if (!isWallSliding)
            {
                isWallSliding = true;
                StartWallSlideSequence();
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

        switch (wallSlidePhase)
        {
            case WallSlidePhase.LerpToZero:
                wallLerpTimer += Time.fixedDeltaTime;
                float lerpT = Mathf.Clamp01(wallLerpTimer / wallLerpToZeroTime);
                float newY = Mathf.Lerp(rb.linearVelocity.y, wallSlideStopSpeed, lerpT);
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, newY, 0f);
                if (lerpT >= 1f) { wallSlidePhase = WallSlidePhase.AcceleratingToSlide; wallSlideDelayTimer = 0f; }
                break;

          //  case WallSlidePhase.WaitingAtZero:
          //      rb.linearVelocity = new Vector3(rb.linearVelocity.x, wallSlideStopSpeed, 0f);
          //      wallSlideDelayTimer += Time.fixedDeltaTime;
          //      if (wallSlideDelayTimer >= wallSlideDelay) { wallSlidePhase = WallSlidePhase.AcceleratingToSlide; wallSlideAccelerationTimer = 0f; }
          //      break;

            case WallSlidePhase.AcceleratingToSlide:
                //rb.linearVelocity = currentVelocity;
                //rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, 0f);
               // rb.lineVelocity.y = 
                wallSlideAccelerationTimer += Time.fixedDeltaTime;
                float accelT = Mathf.Clamp01(wallSlideAccelerationTimer / wallSlideAccelerationTime);
                float acceleratingY = Mathf.Lerp(wallSlideStopSpeed, maxWallSlideSpeed, accelT);
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, acceleratingY, 0f);
                if (accelT >= 1f) { wallSlidePhase = WallSlidePhase.Sliding; }
                break;

            case WallSlidePhase.Sliding:
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, maxWallSlideSpeed, 0f);
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
        float moveInput = Input.GetAxisRaw("Horizontal");
        if (moveInput > 0 && !isFacingRight) Turn();
        else if (moveInput < 0 && isFacingRight) Turn();
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

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector3.right * wallCheckDistance);
        Gizmos.DrawRay(transform.position, Vector3.left * wallCheckDistance);
    }
}
