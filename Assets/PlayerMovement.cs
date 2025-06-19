using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float acceleration = 20f;
    public float deceleration = 25f;
    public float jumpForce = 12f;
    public int maxJumps = 2;

    [Header("Wall Slide Settings")]
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

    private Rigidbody rb;
    private bool isFacingRight = true;
    private int jumpCount;
    public bool isWallSliding;
    private bool isGrounded;
    private float fixedZPosition;

    private bool touchingWallLeft;
    private bool touchingWallRight;

    private float wallLerpTimer;
    private float wallSlideDelayTimer;
    private float wallSlideAccelerationTimer;

    private enum WallSlidePhase { None, LerpToZero, WaitingAtZero, AcceleratingToSlide, Sliding }
    private WallSlidePhase wallSlidePhase = WallSlidePhase.None;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        fixedZPosition = transform.position.z;
        rb.useGravity = true;
    }
    public Vector3 GetVelocity()
    {
        return rb.linearVelocity;
    }
    private void Update()
    {
        HandleInput();
        CheckGround();
        CheckWallContacts();
        CheckWallSlideState();
        Flip();

        currentVelocity = rb.linearVelocity;
    }

    private void FixedUpdate()
    {
        ApplyHorizontalMovement();
        ApplyWallSlideBehavior();

        Vector3 pos = rb.position;
        pos.z = fixedZPosition;
        rb.position = pos;
    }

    private void HandleInput()
    {
        if (Input.GetButtonDown("Jump"))
        {
            if (isGrounded || isWallSliding || jumpCount < maxJumps)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, 0f);
                jumpCount++;
                ResetWallSlide();
            }
        }
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
        rb.linearVelocity = new Vector3(velocityX, rb.linearVelocity.y, 0f);
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
            }
        }
        else if (isWallSliding)
        {
            ResetWallSlide();
        }
    }

    private void StartWallSlideSequence()
    {
        Debug.Log("Started wall sliding.");
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
                float newY = Mathf.Lerp(rb.linearVelocity.y, 0f, lerpT);
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, newY, 0f);

                if (lerpT >= 1f)
                {
                    wallSlidePhase = WallSlidePhase.WaitingAtZero;
                    wallSlideDelayTimer = 0f;
                    Debug.Log("Wall slide paused at zero, starting delay.");
                }
                break;

            case WallSlidePhase.WaitingAtZero:
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, 0f);
                wallSlideDelayTimer += Time.fixedDeltaTime;
                if (wallSlideDelayTimer >= wallSlideDelay)
                {
                    wallSlidePhase = WallSlidePhase.AcceleratingToSlide;
                    wallSlideAccelerationTimer = 0f;
                    Debug.Log("Wall slide accelerating to max speed.");
                }
                break;

            case WallSlidePhase.AcceleratingToSlide:
                wallSlideAccelerationTimer += Time.fixedDeltaTime;
                float accelT = Mathf.Clamp01(wallSlideAccelerationTimer / wallSlideAccelerationTime);
                float acceleratingY = Mathf.Lerp(0f, maxWallSlideSpeed, accelT);
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, acceleratingY, 0f);

                if (accelT >= 1f)
                {
                    wallSlidePhase = WallSlidePhase.Sliding;
                    Debug.Log("Wall slide now fully active at constant speed.");
                }
                break;

            case WallSlidePhase.Sliding:
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, maxWallSlideSpeed, 0f);
                break;
        }
    }

    private void ResetWallSlide()
    {
        isWallSliding = false;
        wallSlidePhase = WallSlidePhase.None;
        wallLerpTimer = 0f;
        wallSlideDelayTimer = 0f;
        wallSlideAccelerationTimer = 0f;
        Debug.Log("Stopped wall sliding.");
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
