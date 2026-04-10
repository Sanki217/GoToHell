using UnityEngine;
using System.Collections;

/// <summary>
/// Wall Jumper — an enemy that clings to a side wall and patrols vertically.
///
/// BEHAVIOUR:
///   • Climbs up and down the side wall it spawns near.
///   • When the player is within yMatchThreshold on the Y axis AND within
///     jumpTriggerRadius, it fires a wall-jump arc toward the player.
///   • If the arc hits a SIDE wall   → snaps on, resumes climbing.
///   • If the arc hits a HORIZONTAL surface (floor / platform)
///       → waits landWaitTime seconds, then:
///           – if player is within aggroRadius: jumps toward the player.
///           – otherwise: jumps toward the nearest side wall.
///   • Repeat.
///
/// SETUP:
///   1. Add Enemy + this script to the prefab.
///   2. Spawn the prefab close to a side wall — Start() auto-detects which wall.
///   3. Set solidLayers to whatever your walls / platforms are on.
///   4. Tune Inspector values to taste.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class EnemyWallJumper : MonoBehaviour
{
    // ================================================================
    //  INSPECTOR
    // ================================================================

    [Header("Wall Climbing")]
    [Tooltip("Vertical patrol speed while on a side wall.")]
    public float climbSpeed = 2.5f;
    [Tooltip("Look-ahead distance for the obstacle box used to reverse climb direction.")]
    public float climbLookAhead = 0.55f;
    [Tooltip("Half-extents of the box used to detect vertical obstacles while climbing.")]
    public Vector3 climbCheckBox = new Vector3(0.42f, 0.32f, 0.1f);
    [Tooltip("Seconds after reversing before the next direction change is allowed.")]
    public float climbReverseCooldown = 0.25f;

    [Header("Jump Trigger")]
    [Tooltip("Max vertical distance (|enemy.y − player.y|) that triggers a wall jump.")]
    public float yMatchThreshold = 1.8f;
    [Tooltip("Max horizontal distance to player that triggers a wall jump.")]
    public float jumpTriggerRadius = 10f;
    [Tooltip("Seconds of cooldown between consecutive jumps (prevents spam).")]
    public float jumpCooldown = 1.2f;

    [Header("Jump Physics")]
    [Tooltip("Horizontal speed component of the wall-jump arc.")]
    public float jumpHorizontalForce = 9f;
    [Tooltip("Initial upward speed component of the wall-jump arc.")]
    public float jumpVerticalForce = 11f;
    [Tooltip("Manual gravity applied each second while airborne.")]
    public float jumpGravity = 25f;

    [Header("Landing on Platform")]
    [Tooltip("Seconds the enemy waits on a horizontal surface before jumping again.")]
    public float landWaitTime = 1f;
    [Tooltip("Player detection radius after a platform landing. " +
             "If player is inside: jump toward player. Otherwise: jump toward nearest side wall.")]
    public float aggroRadius = 9f;

    [Header("Detection")]
    public LayerMask solidLayers;
    [Tooltip("Half-extents of the box used for in-flight collision detection.")]
    public Vector3 flightCastHalf = new Vector3(0.32f, 0.32f, 0.1f);

    [Header("Knockback Wall Bounce")]
    public Vector3 knockbackCastHalf = new Vector3(0.36f, 0.28f, 0.1f);
    [Range(0f, 1f)]
    public float bounceDamping = 0.55f;

    [Header("Freeze (set by FreezeEffect)")]
    [Range(0f, 1f)]
    public float speedMultiplier = 1f;

    // ================================================================
    //  STATE MACHINE
    // ================================================================

    private enum State { Climbing, Jumping, Landed }
    private State state = State.Climbing;

    // Climbing
    private float dirY = 1f;                 // +1 = up, −1 = down
    private float reverseCooldownTimer = 0f;
    private float pathX;                     // X position locked while on a wall

    // Jump direction away from the current wall (+1 = rightward, −1 = leftward)
    private float awayFromWallDir = 1f;

    // In-flight velocity
    private Vector3 vel;

    // Landing / cooldown
    private float landTimer = 0f;
    private float jumpCooldownTimer = 0f;

    // References
    private Transform player;
    private Collider[] selfColliders;

    // ================================================================
    //  INIT
    // ================================================================

    private void Start()
    {
        selfColliders = GetComponentsInChildren<Collider>(true);
        pathX = transform.position.x;
        player = GameObject.FindWithTag("Player")?.transform;

        // Detect which side wall we're closest to and set the default jump direction
        DetectInitialWallSide();
    }

    private void DetectInitialWallSide()
    {
        float distLeft = float.MaxValue, distRight = float.MaxValue;
        if (Physics.Raycast(transform.position, Vector3.left, out RaycastHit hL, 8f,
                            solidLayers, QueryTriggerInteraction.Ignore))
            distLeft = hL.distance;
        if (Physics.Raycast(transform.position, Vector3.right, out RaycastHit hR, 8f,
                            solidLayers, QueryTriggerInteraction.Ignore))
            distRight = hR.distance;

        // Wall is to the left → jump direction is rightward (+1), and vice-versa
        awayFromWallDir = distLeft < distRight ? 1f : -1f;
    }

    // ================================================================
    //  FIXED UPDATE — dispatch to active state
    // ================================================================

    private void FixedUpdate()
    {
        if (jumpCooldownTimer > 0f)
            jumpCooldownTimer -= Time.fixedDeltaTime;

        switch (state)
        {
            case State.Climbing: ClimbTick(); break;
            case State.Jumping:  JumpTick();  break;
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

        // Move
        float nextY = transform.position.y + dirY * climbSpeed * speedMultiplier * dt;
        transform.position = new Vector3(pathX, nextY, 0f);

        // Check jump trigger
        if (player == null || jumpCooldownTimer > 0f) return;

        float dy = Mathf.Abs(player.position.y - transform.position.y);
        float dx = Mathf.Abs(player.position.x - transform.position.x);

        if (dy <= yMatchThreshold && dx <= jumpTriggerRadius)
            LaunchTowardTarget(player.position);
    }

    private bool ClimbObstacleAhead()
    {
        Vector3 dir = new Vector3(0f, dirY, 0f);
        Vector3 center = transform.position + dir * climbLookAhead;
        Collider[] hits = Physics.OverlapBox(center, climbCheckBox,
                                             Quaternion.identity, solidLayers,
                                             QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
            if (!IsSelf(h)) return true;
        return false;
    }

    // ================================================================
    //  JUMPING (in-flight)
    // ================================================================

    private void LaunchTowardTarget(Vector3 target)
    {
        float dx = target.x - transform.position.x;
        // Horizontal direction: toward target, or fall back to away-from-wall default
        float hDir = Mathf.Abs(dx) > 0.15f ? Mathf.Sign(dx) : awayFromWallDir;

        vel = new Vector3(hDir * jumpHorizontalForce, jumpVerticalForce, 0f);
        jumpCooldownTimer = jumpCooldown;
        state = State.Jumping;
    }

    private void JumpTick()
    {
        float dt = Time.fixedDeltaTime;

        // Apply manual gravity
        vel.y -= jumpGravity * dt;
        vel.z  = 0f;

        Vector3 step = vel * dt;

        // ── Side wall collision (horizontal) ──────────────────────────
        if (Mathf.Abs(step.x) > 0.0001f)
        {
            Vector3 hDir  = new Vector3(Mathf.Sign(step.x), 0f, 0f);
            float   hDist = Mathf.Abs(step.x);

            if (Physics.BoxCast(transform.position, flightCastHalf, hDir,
                                out RaycastHit hHit, Quaternion.identity,
                                hDist + 0.06f, solidLayers, QueryTriggerInteraction.Ignore)
                && !IsSelf(hHit.collider))
            {
                // Snap flush to the wall
                float signedHalf = Mathf.Sign(step.x) * flightCastHalf.x;
                pathX = hHit.point.x - signedHalf;
                transform.position = new Vector3(pathX, transform.position.y, 0f);

                // Away-from-wall direction now flips (new wall is on the side we just hit)
                awayFromWallDir = -Mathf.Sign(step.x);

                // Preserve vertical momentum for climb direction
                dirY = vel.y >= 0f ? 1f : -1f;
                reverseCooldownTimer = climbReverseCooldown;
                vel = Vector3.zero;
                state = State.Climbing;
                return;
            }
        }

        // ── Horizontal surface collision (only when falling) ──────────
        if (step.y < 0f)
        {
            float vDist = Mathf.Abs(step.y);

            if (Physics.BoxCast(transform.position, flightCastHalf, Vector3.down,
                                out RaycastHit vHit, Quaternion.identity,
                                vDist + 0.06f, solidLayers, QueryTriggerInteraction.Ignore)
                && !IsSelf(vHit.collider))
            {
                // Snap on top of the surface
                float landY = vHit.point.y + flightCastHalf.y + 0.02f;
                transform.position = new Vector3(transform.position.x, landY, 0f);
                vel = Vector3.zero;

                landTimer = landWaitTime;
                state = State.Landed;
                return;
            }
        }

        // Free movement
        transform.position = new Vector3(
            transform.position.x + step.x,
            transform.position.y + step.y,
            0f);
    }

    // ================================================================
    //  LANDED (on horizontal surface — wait, then re-jump)
    // ================================================================

    private void LandedTick()
    {
        landTimer -= Time.fixedDeltaTime;
        if (landTimer > 0f) return;

        // Jump toward player if close enough
        if (player != null)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= aggroRadius)
            {
                LaunchTowardTarget(player.position);
                return;
            }
        }

        // Otherwise jump toward the nearest side wall
        JumpTowardNearestWall();
    }

    private void JumpTowardNearestWall()
    {
        float distLeft = float.MaxValue, distRight = float.MaxValue;
        if (Physics.Raycast(transform.position, Vector3.left, out RaycastHit hL, 40f,
                            solidLayers, QueryTriggerInteraction.Ignore))
            distLeft = hL.distance;
        if (Physics.Raycast(transform.position, Vector3.right, out RaycastHit hR, 40f,
                            solidLayers, QueryTriggerInteraction.Ignore))
            distRight = hR.distance;

        // Target the centre of whichever wall is closer
        Vector3 target;
        if (distLeft <= distRight)
            target = transform.position + Vector3.left  * distLeft;
        else
            target = transform.position + Vector3.right * distRight;

        LaunchTowardTarget(target);
    }

    // ================================================================
    //  KNOCKBACK
    // ================================================================

    public void ReceiveKnockback(Vector3 impulse, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(KnockbackRoutine(impulse, duration));
    }

    private IEnumerator KnockbackRoutine(Vector3 impulse, float duration)
    {
        State savedState = state;
        state = State.Climbing; // pause normal update while knocked back

        Vector3 kbVel = impulse / Mathf.Max(duration, 0.01f);
        kbVel.z = 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            float scale = 1f - Mathf.Clamp01(elapsed / duration);
            scale *= scale; // ease out
            Vector3 step = kbVel * scale * dt;
            step = BounceStep(step);
            transform.position = new Vector3(
                transform.position.x + step.x,
                transform.position.y + step.y,
                0f);
            yield return null;
        }

        state = savedState;
    }

    private Vector3 BounceStep(Vector3 step)
    {
        if (step.sqrMagnitude < 0.00001f) return step;
        float dist = step.magnitude;
        Vector3 dir = step / dist;

        if (!Physics.BoxCast(transform.position, knockbackCastHalf, dir,
                             out RaycastHit hit, Quaternion.identity, dist,
                             solidLayers, QueryTriggerInteraction.Ignore))
            return step;
        if (IsSelf(hit.collider)) return step;

        float safe = Mathf.Max(0f, hit.distance - 0.05f);
        Vector3 remaining = step - dir * safe;
        Vector3 normal = hit.normal; normal.z = 0f;
        if (normal.sqrMagnitude > 0.001f)
        {
            Vector3 reflected = Vector3.Reflect(remaining, normal.normalized) * bounceDamping;
            reflected.z = 0f;
            return dir * safe + reflected;
        }
        return dir * safe;
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
        // Y-match band
        Gizmos.color = Color.yellow;
        float bw = 1.5f;
        Gizmos.DrawLine(
            new Vector3(transform.position.x - bw, transform.position.y - yMatchThreshold, 0f),
            new Vector3(transform.position.x + bw, transform.position.y - yMatchThreshold, 0f));
        Gizmos.DrawLine(
            new Vector3(transform.position.x - bw, transform.position.y + yMatchThreshold, 0f),
            new Vector3(transform.position.x + bw, transform.position.y + yMatchThreshold, 0f));

        // Jump trigger radius
        Gizmos.color = new Color(1f, 0.55f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, jumpTriggerRadius);

        // Aggro radius (post-landing)
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, aggroRadius);

        // Climb obstacle look-ahead box
        Gizmos.color = Color.cyan;
        Vector3 ahead = transform.position + new Vector3(0f, dirY, 0f) * climbLookAhead;
        Gizmos.DrawWireCube(ahead, climbCheckBox * 2f);
    }
#endif
}
