using UnityEngine;
using System.Collections;

/// <summary>
/// Wall Jumper — clings to a side wall, patrols vertically, and leaps at the player.
///
/// STATE MACHINE:
///   Climbing  — moves up/down along a side wall (X locked).
///               Jumps when player is within Y-match band AND trigger radius.
///   Jumping   — ballistic arc with manual gravity.
///               Side wall hit  → snap on, resume Climbing.
///               Floor/platform hit (while falling) → Landed.
///   Landed    — wait landWaitTime, then re-jump:
///               Aggroed OR player in aggroRadius → jump at player.
///               Otherwise → random upward arc (unpredictable hazard).
///
/// AGGRO:
///   Once the enemy has targeted the player even once, hasAggro is set.
///   While aggroed: Landed always jumps at the player (ignores aggroRadius),
///   and the Y-match band in Climbing is widened.
///
/// KNOCKBACK:
///   Fully suspends FixedUpdate (suspended flag). After the impulse ends the
///   enemy enters Jumping with zero velocity — it falls naturally until wall/floor
///   detection puts it back into Climbing or Landed.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class EnemyWallJumper : MonoBehaviour, IKnockbackReceiver
{
    // ================================================================
    //  INSPECTOR
    // ================================================================

    [Header("Wall Climbing")]
    public float climbSpeed = 2.5f;
    public float climbLookAhead = 0.55f;
    public Vector3 climbCheckBox = new Vector3(0.42f, 0.32f, 0.1f);
    [Tooltip("Seconds after reversing before the next direction check.")]
    public float climbReverseCooldown = 0.25f;

    [Header("Jump Trigger (first contact)")]
    [Tooltip("Max |enemy.y − player.y| to trigger a wall-jump while climbing.")]
    public float yMatchThreshold = 1.8f;
    [Tooltip("Max horizontal distance to player to trigger a wall-jump while climbing.")]
    public float jumpTriggerRadius = 10f;
    [Tooltip("Seconds of cooldown between jumps.")]
    public float jumpCooldown = 1.2f;

    [Header("Aggro (after first player jump)")]
    [Tooltip("While aggroed, the Y-match band is multiplied by this. Higher = jumps earlier.")]
    public float aggroYMultiplier = 2.2f;
    [Tooltip("While aggroed, the trigger radius is multiplied by this.")]
    public float aggroRadiusMultiplier = 1.6f;

    [Header("Jump Physics")]
    public float jumpHorizontalForce = 9f;
    public float jumpVerticalForce   = 11f;
    public float jumpGravity         = 25f;

    [Header("Landing on Platform")]
    [Tooltip("Pause on horizontal surface before re-jumping.")]
    public float landWaitTime = 1f;
    [Tooltip("Player detection radius when landed and NOT yet aggroed.")]
    public float aggroRadius = 9f;

    [Header("Detection Geometry")]
    public LayerMask solidLayers;
    [Tooltip("Half-extents of the box used for in-flight collision detection.")]
    public Vector3 flightCastHalf = new Vector3(0.32f, 0.32f, 0.1f);

    [Header("Knockback")]
    public Vector3 knockbackCastHalf = new Vector3(0.36f, 0.28f, 0.1f);
    [Range(0f, 1f)]
    public float bounceDamping = 0.55f;

    [Header("Freeze (set by FreezeEffect)")]
    [Range(0f, 1f)]
    public float speedMultiplier = 1f;

    [Header("Movement Smoothing")]
    [Tooltip("Time in seconds to reach full climb speed after starting or reversing direction. " +
             "~0.12 gives natural ease-in/out. 0 = instant.")]
    public float climbAccelerationTime = 0.12f;

    // ================================================================
    //  STATE
    // ================================================================

    private enum State { Climbing, Jumping, Landed }
    private State state = State.Climbing;

    // Climbing
    private float dirY = 1f;
    private float reverseCooldownTimer = 0f;
    private float pathX;                     // X position locked while on wall

    // Smoothed climb velocity
    private float smoothedClimbVelY = 0f;
    private float climbSmoothDampVel = 0f;

    // Jump direction away from current wall
    private float awayFromWallDir = 1f;

    // In-flight velocity
    private Vector3 vel;

    // Timers
    private float landTimer      = 0f;
    private float jumpCooldownTimer = 0f;

    // Aggro tracking
    private bool hasAggro = false;

    // Knockback suspend — gates ALL of FixedUpdate
    private bool suspended = false;

    // Refs
    private Transform player;
    private Collider[] selfColliders;

    // Reusable hit buffer — shared across all wall jumpers, avoids per-frame allocation
    private static readonly Collider[] hitBuffer = new Collider[8];

    // ================================================================
    //  INIT
    // ================================================================

    private void Start()
    {
        selfColliders = GetComponentsInChildren<Collider>(true);
        pathX = transform.position.x;
        player = PlayerRefs.I?.T;
        DetectInitialWallSide();
    }

    private void DetectInitialWallSide()
    {
        float dL = float.MaxValue, dR = float.MaxValue;
        if (Physics.Raycast(transform.position, Vector3.left,  out RaycastHit hL, 8f,
                            solidLayers, QueryTriggerInteraction.Ignore)) dL = hL.distance;
        if (Physics.Raycast(transform.position, Vector3.right, out RaycastHit hR, 8f,
                            solidLayers, QueryTriggerInteraction.Ignore)) dR = hR.distance;

        // Wall on the left → jump rightward (+1); wall on the right → jump leftward (−1)
        awayFromWallDir = dL < dR ? 1f : -1f;
    }

    // ================================================================
    //  FIXED UPDATE
    // ================================================================

    private void FixedUpdate()
    {
        // Knockback coroutine holds this flag — nothing moves while it's set
        if (suspended) return;

        if (jumpCooldownTimer > 0f)
            jumpCooldownTimer -= Time.fixedDeltaTime;

        switch (state)
        {
            case State.Climbing: ClimbTick();  break;
            case State.Jumping:  JumpTick();   break;
            case State.Landed:   LandedTick(); break;
        }
    }

    // ================================================================
    //  CLIMBING
    // ================================================================

    private void ClimbTick()
    {
        float dt = Time.fixedDeltaTime;

        // Reverse at vertical obstacle
        reverseCooldownTimer -= dt;
        if (reverseCooldownTimer <= 0f && ClimbObstacleAhead())
        {
            dirY *= -1f;
            reverseCooldownTimer = climbReverseCooldown;
        }

        // Smooth climb: ease in/out on direction reversals
        float targetClimbVel = dirY * climbSpeed * speedMultiplier;
        float smoothTime = climbAccelerationTime > 0f ? climbAccelerationTime : 0.001f;
        smoothedClimbVelY = Mathf.SmoothDamp(smoothedClimbVelY, targetClimbVel,
                                              ref climbSmoothDampVel, smoothTime,
                                              float.MaxValue, dt);
        float nextY = transform.position.y + smoothedClimbVelY * dt;
        transform.position = new Vector3(pathX, nextY, 0f);

        // ── Jump trigger check ────────────────────────────────────────
        if (player == null || jumpCooldownTimer > 0f) return;

        float dy = Mathf.Abs(player.position.y - transform.position.y);
        float dx = Mathf.Abs(player.position.x - transform.position.x);

        float yBand   = hasAggro ? yMatchThreshold * aggroYMultiplier   : yMatchThreshold;
        float xRadius = hasAggro ? jumpTriggerRadius * aggroRadiusMultiplier : jumpTriggerRadius;

        if (dy <= yBand && dx <= xRadius)
            LaunchTowardTarget(player.position, isPlayerTarget: true);
    }

    private bool ClimbObstacleAhead()
    {
        Vector3 dir    = new Vector3(0f, dirY, 0f);
        Vector3 center = transform.position + dir * climbLookAhead;
        int count = Physics.OverlapBoxNonAlloc(center, climbCheckBox, hitBuffer,
                                             Quaternion.identity, solidLayers,
                                             QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
            if (!IsSelf(hitBuffer[i])) return true;
        return false;
    }

    // ================================================================
    //  JUMPING (in-flight ballistic arc)
    // ================================================================

    private void LaunchTowardTarget(Vector3 target, bool isPlayerTarget = false)
    {
        float dx   = target.x - transform.position.x;
        float hDir = Mathf.Abs(dx) > 0.15f ? Mathf.Sign(dx) : awayFromWallDir;

        vel = new Vector3(hDir * jumpHorizontalForce, jumpVerticalForce, 0f);
        jumpCooldownTimer = jumpCooldown;

        if (isPlayerTarget) hasAggro = true;

        state = State.Jumping;
    }

    private void LaunchRandom()
    {
        // Unpredictable arc — random horizontal direction, always upward
        float hDir = Random.value > 0.5f ? 1f : -1f;
        vel = new Vector3(hDir * jumpHorizontalForce, jumpVerticalForce, 0f);
        jumpCooldownTimer = jumpCooldown;
        state = State.Jumping;
    }

    private void JumpTick()
    {
        float dt = Time.fixedDeltaTime;

        vel.y -= jumpGravity * speedMultiplier * dt;
        vel.z  = 0f;

        Vector3 step = vel * dt;

        // ── Side-wall hit ─────────────────────────────────────────────
        if (Mathf.Abs(step.x) > 0.0001f)
        {
            Vector3 hDir  = new Vector3(Mathf.Sign(step.x), 0f, 0f);
            float   hDist = Mathf.Abs(step.x);

            if (Physics.BoxCast(transform.position, flightCastHalf, hDir,
                                out RaycastHit hHit, Quaternion.identity,
                                hDist + 0.06f, solidLayers, QueryTriggerInteraction.Ignore)
                && !IsSelf(hHit.collider))
            {
                float signedHalf = Mathf.Sign(step.x) * flightCastHalf.x;
                pathX = hHit.point.x - signedHalf;
                transform.position = new Vector3(pathX, transform.position.y, 0f);

                awayFromWallDir = -Mathf.Sign(step.x); // update jump direction for new wall
                dirY = vel.y >= 0f ? 1f : -1f;
                reverseCooldownTimer = climbReverseCooldown;
                vel = Vector3.zero;
                state = State.Climbing;
                return;
            }
        }

        // ── Horizontal-surface hit (only while falling) ───────────────
        if (step.y < 0f)
        {
            float vDist = Mathf.Abs(step.y);

            if (Physics.BoxCast(transform.position, flightCastHalf, Vector3.down,
                                out RaycastHit vHit, Quaternion.identity,
                                vDist + 0.06f, solidLayers, QueryTriggerInteraction.Ignore)
                && !IsSelf(vHit.collider))
            {
                float landY = vHit.point.y + flightCastHalf.y + 0.02f;
                transform.position = new Vector3(transform.position.x, landY, 0f);
                vel = Vector3.zero;

                landTimer = landWaitTime;
                state = State.Landed;
                return;
            }
        }

        // Free flight
        transform.position = new Vector3(
            transform.position.x + step.x,
            transform.position.y + step.y,
            0f);
    }

    // ================================================================
    //  LANDED (on horizontal surface — pause then re-jump)
    // ================================================================

    private void LandedTick()
    {
        landTimer -= Time.fixedDeltaTime;
        if (landTimer > 0f) return;

        if (player != null)
        {
            // Aggroed: always chase player regardless of distance
            // Not yet aggroed: chase only if within aggroRadius
            float dist = Vector3.Distance(transform.position, player.position);
            bool shouldChase = hasAggro || dist <= aggroRadius;

            if (shouldChase)
            {
                LaunchTowardTarget(player.position, isPlayerTarget: true);
                return;
            }
        }

        // No player target — jump in a random direction
        LaunchRandom();
    }

    // ================================================================
    //  KNOCKBACK — suspends FixedUpdate entirely while active
    // ================================================================

    public void ReceiveKnockback(Vector3 impulse, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(KnockbackRoutine(impulse, duration));
    }

    private IEnumerator KnockbackRoutine(Vector3 impulse, float duration)
    {
        // Cancel any in-flight arc and freeze FixedUpdate
        vel = Vector3.zero;
        suspended = true;

        Vector3 kbVel = impulse / Mathf.Max(duration, 0.01f);
        kbVel.z = 0f;

        // Remember horizontal direction so we can keep moving that way after knockback
        float kbHorizDir = Mathf.Abs(kbVel.x) > 0.1f ? Mathf.Sign(kbVel.x) : 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            float scale = 1f - Mathf.Clamp01(elapsed / duration);
            scale *= scale; // ease out

            Vector3 step = kbVel * scale * dt;
            step = KnockbackBouncer.StepWithWallBounce(
                step, transform.position, knockbackCastHalf,
                bounceDamping, solidLayers, selfColliders);
            transform.position = new Vector3(
                transform.position.x + step.x,
                transform.position.y + step.y,
                0f);
            yield return null;
        }

        // Seed horizontal velocity in the knockback direction so the enemy arcs away
        // rather than falling straight down. JumpTick applies gravity and detects landing.
        vel   = new Vector3(kbHorizDir * jumpHorizontalForce, 0f, 0f);
        state = State.Jumping;
        smoothedClimbVelY = 0f;
        climbSmoothDampVel = 0f;
        suspended = false;
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    private bool IsSelf(Collider c)
    {
        foreach (var sc in selfColliders)
            if (sc == c) return true;
        return false;
    }

    // ================================================================
    //  GIZMOS
    // ================================================================

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Y-match trigger band
        Gizmos.color = Color.yellow;
        float bw = 1.5f;
        float yBand = Application.isPlaying && hasAggro
            ? yMatchThreshold * aggroYMultiplier : yMatchThreshold;
        Gizmos.DrawLine(new Vector3(transform.position.x - bw, transform.position.y - yBand,  0f),
                        new Vector3(transform.position.x + bw, transform.position.y - yBand,  0f));
        Gizmos.DrawLine(new Vector3(transform.position.x - bw, transform.position.y + yBand,  0f),
                        new Vector3(transform.position.x + bw, transform.position.y + yBand,  0f));

        // Jump trigger radius
        float xRadius = Application.isPlaying && hasAggro
            ? jumpTriggerRadius * aggroRadiusMultiplier : jumpTriggerRadius;
        Gizmos.color = new Color(1f, 0.55f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, xRadius);

        // Aggro radius (pre-aggro landing detection)
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.22f);
        Gizmos.DrawWireSphere(transform.position, aggroRadius);

        // Climb obstacle box
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(
            transform.position + new Vector3(0f, dirY, 0f) * climbLookAhead,
            climbCheckBox * 2f);
    }
#endif
}
