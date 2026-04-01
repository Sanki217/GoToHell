using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Enemy))]
public class EnemyShooter : MonoBehaviour
{
    [Header("Detection")]
    public float attackRange = 15f;

    [Header("Shooting")]
    public GameObject projectilePrefab;
    public float attackCooldown = 2f;
    public float projectileSpeed = 10f;
    public float muzzleOffset = 0.7f;

    [Header("Behaviour")]
    public bool facesPlayer = false;

    [Header("Knockback Return")]
    [Tooltip("Speed at which the shooter smoothly returns to its home position")]
    public float returnSpeed = 4f;
    [Tooltip("Half-extents of the BoxCast for wall detection during knockback")]
    public Vector3 knockbackCastHalf = new Vector3(0.3f, 0.4f, 0.1f);
    [Range(0f, 1f)]
    public float bounceDamping = 0.5f;

    // ================================================================
    //  STATE
    // ================================================================

    private Vector3 homePosition;
    private float cooldownTimer = 0f;
    private Transform player;

    private bool knockedBack = false;
    private bool returning = false;
    private float returnVelX = 0f;
    private float returnVelY = 0f;

    private Collider[] selfColliders;

    // ================================================================
    //  INIT
    // ================================================================

    private void Start()
    {
        homePosition = transform.position;
        selfColliders = GetComponentsInChildren<Collider>(true);

        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null) player = playerGO.transform;

        cooldownTimer = Random.Range(0f, attackCooldown);
    }

    // ================================================================
    //  UPDATE
    // ================================================================

    private void Update()
    {
        if (knockedBack) return;

        // Smooth return to home after knockback
        if (returning)
        {
            float x = Mathf.SmoothDamp(transform.position.x, homePosition.x,
                                        ref returnVelX, 0.25f, returnSpeed, Time.deltaTime);
            float y = Mathf.SmoothDamp(transform.position.y, homePosition.y,
                                        ref returnVelY, 0.25f, returnSpeed, Time.deltaTime);
            transform.position = new Vector3(x, y, 0f);

            float dist = Vector3.Distance(transform.position, homePosition);
            if (dist < 0.05f)
            {
                transform.position = homePosition;
                returning = false;
                returnVelX = 0f;
                returnVelY = 0f;
            }
        }

        // Shooting
        if (player == null || projectilePrefab == null) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.z = 0f;
        if (toPlayer.magnitude > attackRange) return;

        if (facesPlayer && toPlayer.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        cooldownTimer += Time.deltaTime;
        if (cooldownTimer < attackCooldown) return;

        cooldownTimer = 0f;
        Shoot(toPlayer.normalized);
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
        knockedBack = true;
        returning = false;
        returnVelX = 0f;
        returnVelY = 0f;

        Vector3 velocity = impulse / Mathf.Max(duration, 0.01f);
        velocity.z = 0f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;

            float t = Mathf.Clamp01(elapsed / duration);
            float scale = 1f - t * t;
            Vector3 step = velocity * scale * dt;

            step = StepWithWallBounce(step);

            transform.position = new Vector3(
                transform.position.x + step.x,
                transform.position.y + step.y,
                0f);

            yield return null;
        }

        knockedBack = false;
        returning = true;
    }

    private Vector3 StepWithWallBounce(Vector3 step)
    {
        if (step.sqrMagnitude < 0.00001f) return step;

        float dist = step.magnitude;
        Vector3 dir = step / dist;

        // Use solidLayers from whichever patrol script is present, fallback to wall layer
        LayerMask mask = LayerMask.GetMask("Wall", "Ground");

        bool found = Physics.BoxCast(
            transform.position,
            knockbackCastHalf,
            dir,
            out RaycastHit hit,
            Quaternion.identity,
            dist,
            mask,
            QueryTriggerInteraction.Ignore);

        if (!found) return step;

        bool isSelf = false;
        foreach (var sc in selfColliders)
            if (sc == hit.collider) { isSelf = true; break; }
        if (isSelf) return step;

        float safe = Mathf.Max(0f, hit.distance - 0.05f);
        Vector3 safeStep = dir * safe;
        Vector3 remaining = step - safeStep;
        Vector3 normal = hit.normal; normal.z = 0f;

        if (normal.sqrMagnitude > 0.001f)
        {
            Vector3 reflected = Vector3.Reflect(remaining, normal.normalized) * bounceDamping;
            reflected.z = 0f;
            return safeStep + reflected;
        }

        return safeStep;
    }

    // ================================================================
    //  SHOOT
    // ================================================================

    private void Shoot(Vector3 dir)
    {
        Vector3 pos = transform.position + dir * muzzleOffset;
        GameObject proj = Instantiate(projectilePrefab, pos, Quaternion.identity);
        proj.GetComponent<EnemyProjectile>()?.Initialize(dir, projectileSpeed, transform.root);
    }

    // ================================================================
    //  GIZMOS
    // ================================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(homePosition, 0.2f);
        }
    }
}