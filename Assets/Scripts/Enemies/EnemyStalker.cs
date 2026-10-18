using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum StalkerRole
{
    /// <summary>Hunts the player down and lunges into him.</summary>
    Melee,
    /// <summary>Hovers at range from the player (on the side it came from) and shoots.</summary>
    Shooter
}

public enum StalkerTerrain
{
    /// <summary>Phases through walls and floors (never gets stuck). Shooters still need line of sight to fire.</summary>
    PassThrough,
    /// <summary>Solid like the player — slides along walls. Can be cornered.</summary>
    Blocked
}

/// <summary>
/// Flying stalker. Spawned by EnemyStalkerSpawner in front of the level
/// (negative Z, toward the camera) and flies onto the gameplay plane (z = 0).
///
/// ROLES (one script, pick in the Inspector):
///   • MELEE — always goes for the player. Chases, and once within
///     lungeRange it telegraphs (tint + small pull-back for lungeWindup)
///     then lunges in a straight line. After a lunge, or after touching the
///     player, it recoils and recovers before chasing again — so it hits,
///     backs off, and comes back instead of sitting inside the player.
///     Contact damage: child trigger collider with EnemyDamage.
///   • SHOOTER — keeps preferredRange from the player in the direction it
///     came from (any angle), gently bobbing. Retreats when the player
///     closes in. Only fires with line of sight and never from inside a
///     wall; each shot is telegraphed for shotWindup.
///
/// SPEED: lerps minSpeed → maxSpeed as the distance to the player grows
/// from slowDistance to fastDistance — far stalkers catch up fast, near
/// ones are readable.
///
/// DEPTH (Z): the further from the player, the deeper toward the camera
/// (−Z, up to maxDepth); at depthNearDistance or closer it is on the
/// gameplay plane (z = 0) and can attack / be hit.
///
/// TERRAIN: PassThrough (default) or Blocked — see StalkerTerrain. While
/// still flying in, or if it ends up overlapping a wall, it phases until free.
///
/// Stalkers push away from each other (separation) so they don't stack.
/// Freeze / slow statuses scale everything through Enemy.SpeedMultiplier.
///
/// Prefab: tag "Enemy", enemy layer, mesh, Rigidbody (kinematic), collider,
/// Enemy, EnemyHealthBar, this script; melee also a child trigger with
/// EnemyDamage. Old prefabs migrate automatically (projectile assigned →
/// Shooter, otherwise Melee).
/// </summary>
[RequireComponent(typeof(Enemy))]
public class EnemyStalker : MonoBehaviour, IKnockbackReceiver, ISerializationCallbackReceiver
{
    [Header("Role")]
    public StalkerRole role = StalkerRole.Melee;
    public StalkerTerrain terrain = StalkerTerrain.PassThrough;
    [Tooltip("Body radius for wall checks and line of sight.")]
    public float bodyRadius = 0.4f;

    [Header("Movement")]
    [FormerlySerializedAs("moveSpeed")]
    [Tooltip("Speed when close to the player (at slowDistance or nearer).")]
    public float minSpeed = 8f;
    [Tooltip("Speed when far from the player (at fastDistance or further).")]
    public float maxSpeed = 22f;
    public float slowDistance = 4f;
    public float fastDistance = 20f;
    [Tooltip("How fast it changes velocity — lower = floatier.")]
    public float acceleration = 20f;

    [Header("Depth (Z) by distance")]
    [Tooltip("Deepest −Z it goes when far away.")]
    public float maxDepth = 10f;
    [Tooltip("At this distance or closer it is on the gameplay plane (z = 0). Keep ≥ preferredRange for shooters.")]
    public float depthNearDistance = 8f;
    [Tooltip("At this distance or further it is at maxDepth.")]
    public float depthFarDistance = 22f;
    [FormerlySerializedAs("flyInSpeed")]
    [Tooltip("How fast Z follows its target depth.")]
    public float depthSpeed = 10f;
    [Tooltip("Aim point above the player's pivot (his body centre).")]
    public float heightOffset = 0.5f;
    [Tooltip("Stalkers closer than this push each other apart.")]
    public float separationRadius = 1.5f;
    public float separationStrength = 6f;

    [Header("Melee")]
    [Tooltip("Starts the lunge telegraph within this distance.")]
    public float lungeRange = 4f;
    [Tooltip("Telegraph time before the lunge — the player's window to react.")]
    public float lungeWindup = 0.35f;
    public float lungeSpeed = 18f;
    public float lungeDuration = 0.3f;
    [Tooltip("Drift time after a lunge or a hit before it chases again.")]
    public float lungeRecovery = 0.6f;
    [Tooltip("Minimum time between lunges.")]
    public float lungeCooldown = 1.5f;
    [Tooltip("Push-back speed after it touches the player.")]
    public float hitRecoilSpeed = 9f;

    [Header("Shooter")]
    [FormerlySerializedAs("followDistance")]
    [Tooltip("Horizontal distance kept from the player.")]
    public float preferredRange = 7f;
    public float bobAmplitude = 0.4f;
    public float bobFrequency = 0.6f;
    public GameObject projectilePrefab;
    public float attackCooldown = 2f;
    [Tooltip("Telegraph time before each shot.")]
    public float shotWindup = 0.4f;
    public float projectileSpeed = 10f;
    public float attackRange = 14f;
    public float muzzleOffset = 0.7f;
    [Tooltip("How close to the gameplay plane (z = 0) it must be before it may attack.")]
    public float shootZTolerance = 0.5f;

    [Header("Telegraph")]
    public Color windupColor = new Color(1f, 0.25f, 0.15f);

    [Header("Knockback")]
    public Vector3 knockbackCastHalf = new Vector3(0.3f, 0.4f, 0.1f);
    [Range(0f, 1f)] public float bounceDamping = 0.5f;

    [SerializeField, HideInInspector] private int dataVersion;

    private enum State { Chase, Windup, Lunge, Recover }

    private const float ArriveGain = 3f;
    private const float Skin = 0.02f;
    private const float HoverResolveInterval = 0.25f;

    private static readonly List<EnemyStalker> Active = new List<EnemyStalker>();

    // ── state ───────────────────────────────────────────────────────
    private Enemy enemy;
    private Collider[] selfColliders;
    private EnemyDamage[] contactDamage;
    private State state;
    private float stateTimer;
    private float attackTimer;
    private Vector2 velocity;
    private Vector2 lungeDir;
    private Vector2 approachDir = Vector2.up;   // from player toward us; picked once
    private bool approachChosen;
    private Vector2 hoverOffset;
    private float hoverResolveTimer;
    private float bobPhase;
    private bool knockedBack;

    // ================================================================
    //  LIFECYCLE
    // ================================================================

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        selfColliders = GetComponentsInChildren<Collider>(true);
        contactDamage = GetComponentsInChildren<EnemyDamage>(true);
        bobPhase = Random.value * Mathf.PI * 2f;
    }

    private void OnEnable()
    {
        Active.Add(this);
        foreach (EnemyDamage d in contactDamage) d.OnHitPlayer += HandleHitPlayer;
        state = State.Chase;
        attackTimer = role == StalkerRole.Shooter ? Random.Range(0f, attackCooldown) : 0f;
    }

    private void OnDisable()
    {
        Active.Remove(this);
        foreach (EnemyDamage d in contactDamage) d.OnHitPlayer -= HandleHitPlayer;
        StopAllCoroutines();
        knockedBack = false;
        ClearTelegraph();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Active.Clear();

    private float SpeedMultiplier => enemy != null ? enemy.SpeedMultiplier : 1f;

    // ================================================================
    //  UPDATE
    // ================================================================

    private void Update()
    {
        Transform player = PlayerRefs.PlayerTransform;
        if (knockedBack || player == null) return;

        // "Enemy time" — freeze / slow scale every timer and movement.
        float dt = Time.deltaTime * SpeedMultiplier;
        if (dt <= 0f) return;

        Vector3 pos = transform.position;
        Vector2 here = pos;
        Vector2 target = (Vector2)player.position + Vector2.up * heightOffset;

        Vector2 fromPlayer = here - target;
        float distance = fromPlayer.magnitude;
        if (!approachChosen && distance > 0.01f)
        {
            approachDir = fromPlayer / distance;   // the direction we came from
            approachChosen = true;
        }

        float z = Mathf.MoveTowards(pos.z, DepthFor(distance), depthSpeed * dt);
        bool onPlane = Mathf.Abs(z) <= shootZTolerance;

        attackTimer -= dt;
        stateTimer -= dt;

        Vector2 desired = role == StalkerRole.Melee
            ? MeleeDesired(here, target, onPlane)
            : ShooterDesired(here, target, onPlane, dt);

        if (state != State.Lunge)
        {
            desired += Separation(here);
            // Acceleration grows with target speed so fast catch-up stays snappy
            float accel = acceleration * Mathf.Max(1f, desired.magnitude / Mathf.Max(0.01f, minSpeed));
            velocity = Vector2.MoveTowards(velocity, desired, accel * dt);
        }

        Vector2 step = velocity * dt;
        if (terrain == StalkerTerrain.Blocked && onPlane && !InsideSolid(pos, bodyRadius))
            step = Slide(pos, step);

        transform.position = new Vector3(here.x + step.x, here.y + step.y, z);
    }

    // ================================================================
    //  MELEE — chase → windup → lunge → recover
    // ================================================================

    private Vector2 MeleeDesired(Vector2 here, Vector2 target, bool onPlane)
    {
        Vector2 toPlayer = target - here;
        float dist = toPlayer.magnitude;
        Vector2 dir = dist > 0.001f ? toPlayer / dist : -approachDir;

        switch (state)
        {
            case State.Chase:
                if (onPlane && dist <= lungeRange && attackTimer <= 0f && CanReach(here, target))
                {
                    SetState(State.Windup, lungeWindup);
                    SetTelegraph();
                }
                return dir * SpeedFor(dist);   // full speed — it always goes for the player

            case State.Windup:
                if (stateTimer <= 0f)
                {
                    lungeDir = dir;   // aimed at the end of the telegraph
                    velocity = lungeDir * lungeSpeed;
                    SetState(State.Lunge, lungeDuration);
                    ClearTelegraph();
                }
                return -dir * (minSpeed * 0.25f);   // small pull-back = readable anticipation

            case State.Lunge:
                velocity = lungeDir * lungeSpeed;
                if (stateTimer <= 0f) EndAttack();
                return velocity;

            default: // Recover
                if (stateTimer <= 0f) state = State.Chase;
                return Vector2.zero;
        }
    }

    private void HandleHitPlayer(PlayerHealth _)
    {
        if (role != StalkerRole.Melee || !isActiveAndEnabled) return;
        Transform player = PlayerRefs.PlayerTransform;
        Vector2 away = player != null ? (Vector2)(transform.position - player.position) : Vector2.up;
        if (away.sqrMagnitude < 0.0001f) away = Vector2.up;
        velocity = away.normalized * hitRecoilSpeed;
        ClearTelegraph();
        EndAttack();
    }

    private void EndAttack()
    {
        SetState(State.Recover, lungeRecovery);
        attackTimer = lungeCooldown;
    }

    // ================================================================
    //  SHOOTER — hover at range, telegraph, fire
    // ================================================================

    private Vector2 ShooterDesired(Vector2 here, Vector2 target, bool onPlane, float dt)
    {
        hoverResolveTimer -= dt;
        if (hoverResolveTimer <= 0f)
        {
            hoverResolveTimer = HoverResolveInterval;
            hoverOffset = ResolveHoverOffset(target);
        }

        float bob = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f + bobPhase) * bobAmplitude;
        Vector2 hover = target + hoverOffset + Vector2.up * bob;
        Vector2 desired = Arrive(here, hover, SpeedFor((target - here).magnitude));

        Vector3 muzzleFrom = new Vector3(here.x, here.y, 0f);
        bool canShoot = onPlane && projectilePrefab != null
                        && (target - here).sqrMagnitude <= attackRange * attackRange
                        && HasLineOfSight(muzzleFrom, target);

        switch (state)
        {
            case State.Windup:
                if (stateTimer <= 0f)
                {
                    ClearTelegraph();
                    if (canShoot) Fire(here, target);
                    state = State.Chase;
                    attackTimer = attackCooldown;
                }
                return desired * 0.3f;   // slows while aiming

            default:
                state = State.Chase;
                if (canShoot && attackTimer <= 0f)
                {
                    SetState(State.Windup, shotWindup);
                    SetTelegraph();
                }
                return desired;
        }
    }

    private static readonly float[] HoverAngles = { 0f, 30f, -30f, 60f, -60f, 90f, -90f, 135f, -135f, 180f };

    /// <summary>Spot at preferredRange in our approach direction; if it's inside terrain or blind, rotate around the player.</summary>
    private Vector2 ResolveHoverOffset(Vector2 target)
    {
        foreach (float angle in HoverAngles)
        {
            Vector2 offset = (Vector2)(Quaternion.Euler(0f, 0f, angle) * approachDir) * preferredRange;
            Vector3 spot = new Vector3(target.x + offset.x, target.y + offset.y, 0f);
            if (!InsideSolid(spot, bodyRadius) && HasLineOfSight(spot, target))
                return offset;
        }
        return approachDir * preferredRange;
    }

    private void Fire(Vector2 here, Vector2 target)
    {
        Vector2 dir = (target - here).normalized;
        Vector3 pos = new Vector3(here.x, here.y, 0f) + (Vector3)dir * muzzleOffset;
        GameObject proj = Pool.Spawn(projectilePrefab, pos, Quaternion.identity);
        if (proj != null && proj.TryGetComponent(out EnemyProjectile p))
            p.Initialize(dir, projectileSpeed, transform);
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    /// <summary>minSpeed near the player → maxSpeed far away.</summary>
    public float SpeedFor(float distance)
        => Mathf.Lerp(minSpeed, maxSpeed, Mathf.InverseLerp(slowDistance, fastDistance, distance));

    /// <summary>Target Z for a distance from the player: 0 when near, −maxDepth when far.</summary>
    public float DepthFor(float distance)
        => -maxDepth * Mathf.InverseLerp(depthNearDistance, depthFarDistance, distance);

    /// <summary>Puts it straight at the right depth for where it is (spawner calls this — no pop-in).</summary>
    public void SnapDepth()
    {
        Transform player = PlayerRefs.PlayerTransform;
        if (player == null) return;
        Vector3 p = transform.position;
        float d = Vector2.Distance(p, (Vector2)player.position + Vector2.up * heightOffset);
        transform.position = new Vector3(p.x, p.y, DepthFor(d));
    }

    private void SetState(State s, float duration)
    {
        state = s;
        stateTimer = duration;
    }

    private static Vector2 Arrive(Vector2 from, Vector2 to, float maxSpeed)
    {
        Vector2 delta = to - from;
        float dist = delta.magnitude;
        if (dist < 0.001f) return Vector2.zero;
        return delta / dist * Mathf.Min(maxSpeed, dist * ArriveGain);
    }

    private Vector2 Separation(Vector2 here)
    {
        Vector2 push = Vector2.zero;
        float r2 = separationRadius * separationRadius;
        foreach (EnemyStalker other in Active)
        {
            if (other == this) continue;
            Vector2 d = here - (Vector2)other.transform.position;
            float m2 = d.sqrMagnitude;
            if (m2 >= r2 || m2 < 0.0001f) continue;
            float m = Mathf.Sqrt(m2);
            push += d / m * (1f - m / separationRadius);
        }
        return push * separationStrength;
    }

    /// <summary>Melee lunges only when a straight line could reach (always true when phasing).</summary>
    private bool CanReach(Vector2 here, Vector2 target)
        => terrain == StalkerTerrain.PassThrough || HasLineOfSight(new Vector3(here.x, here.y, 0f), target);

    private bool HasLineOfSight(Vector3 from, Vector2 target)
    {
        if (InsideSolid(from, bodyRadius * 0.5f)) return false;   // never shoot out of a wall
        return !Physics.Linecast(from, new Vector3(target.x, target.y, 0f), Layers.SolidMask, QueryTriggerInteraction.Ignore);
    }

    private static bool InsideSolid(Vector3 at, float radius)
        => Physics.CheckSphere(new Vector3(at.x, at.y, 0f), radius, Layers.SolidMask, QueryTriggerInteraction.Ignore);

    /// <summary>Moves like a solid body: stops at walls and slides along them.</summary>
    private Vector2 Slide(Vector3 pos, Vector2 step)
    {
        Vector3 origin = new Vector3(pos.x, pos.y, 0f);
        Vector3 remaining = step;
        Vector3 moved = Vector3.zero;

        for (int i = 0; i < 3; i++)
        {
            float dist = remaining.magnitude;
            if (dist < 0.0001f) break;
            Vector3 dir = remaining / dist;

            if (!Physics.SphereCast(origin + moved, bodyRadius, dir, out RaycastHit hit, dist + Skin,
                                    Layers.SolidMask, QueryTriggerInteraction.Ignore))
            {
                moved += remaining;
                break;
            }

            Vector3 allowed = dir * Mathf.Max(0f, hit.distance - Skin);
            moved += allowed;

            Vector3 n = hit.normal; n.z = 0f;
            if (n.sqrMagnitude < 0.0001f) break;
            n.Normalize();
            remaining = Vector3.ProjectOnPlane(remaining - allowed, n);
            remaining.z = 0f;
            velocity = Vector3.ProjectOnPlane(velocity, n);
        }
        return moved;
    }

    private void SetTelegraph()
    {
        if (enemy != null) enemy.Tint.SetTint(this, windupColor);
    }

    private void ClearTelegraph()
    {
        if (enemy != null) enemy.Tint.ClearTint(this);
    }

    // ================================================================
    //  KNOCKBACK — wall-bounce, then resume (cancels any attack)
    // ================================================================

    public void ReceiveKnockback(Vector3 impulse, float duration)
    {
        if (!isActiveAndEnabled) return;
        StopAllCoroutines();
        StartCoroutine(KnockbackRoutine(impulse, duration));
    }

    private IEnumerator KnockbackRoutine(Vector3 impulse, float duration)
    {
        knockedBack = true;
        velocity = Vector2.zero;
        ClearTelegraph();
        if (state != State.Chase) EndAttack();

        Vector3 v = impulse / Mathf.Max(duration, 0.01f);
        v.z = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;

            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 step = v * (1f - t * t) * dt;

            step = KnockbackBouncer.StepWithWallBounce(
                step, transform.position, knockbackCastHalf,
                bounceDamping, Layers.SolidMask, selfColliders);

            transform.position += new Vector3(step.x, step.y, 0f);
            yield return null;
        }

        knockedBack = false;
    }

    // ================================================================
    //  MIGRATION — old prefabs had no role field
    // ================================================================

    public void OnBeforeSerialize() { }

    public void OnAfterDeserialize()
    {
        if (dataVersion >= 1) return;
        role = !ReferenceEquals(projectilePrefab, null) ? StalkerRole.Shooter : StalkerRole.Melee;
        dataVersion = 1;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 p = transform.position;
        if (role == StalkerRole.Melee)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(p, lungeRange);
        }
        else
        {
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.6f);
            Gizmos.DrawWireSphere(p, attackRange);
        }
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(p, bodyRadius);
    }
}
