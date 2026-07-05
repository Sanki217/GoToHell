using UnityEngine;
using System.Collections;

/// <summary>
/// Knockback dispatch + wall depenetration for one enemy. Split out of the
/// old Enemy god class; added automatically at runtime by Enemy (no prefab
/// changes needed) and configured from Enemy's serialized fields.
///
/// PERF: depenetration used to run every physics frame for every enemy.
/// Wall-embedding only happens around knockback, so it now runs only during
/// a short window after each knockback (plus briefly after spawn). Enemies
/// that need the old always-on behaviour can tick Enemy.alwaysDepenetrate.
/// </summary>
public class EnemyKnockback : MonoBehaviour
{
    // Configured by Enemy.Configure — not serialized here so all tuning
    // stays on the Enemy component (existing prefabs keep their values).
    private LayerMask wallLayers;
    private Vector3 wallCheckHalfExtents;
    private float bounceDamping;
    private float knockbackDuration;
    private bool alwaysDepenetrate;
    private IKnockbackReceiver[] receivers;
    private Collider[] selfColliders;

    private float depenTimer;
    private const float DepenWindowAfterKnockback = 0.6f;
    private const float DepenWindowAfterSpawn = 0.25f;

    private static readonly Collider[] depenBuffer = new Collider[8];

    public void Configure(LayerMask walls, Vector3 halfExtents, float damping,
                          float duration, bool alwaysDepen,
                          IKnockbackReceiver[] knockbackReceivers, Collider[] self)
    {
        wallLayers = walls;
        wallCheckHalfExtents = halfExtents;
        bounceDamping = damping;
        knockbackDuration = duration;
        alwaysDepenetrate = alwaysDepen;
        receivers = knockbackReceivers;
        selfColliders = self;

        depenTimer = DepenWindowAfterSpawn;   // cover spawn overlap edge cases
    }

    // ================================================================
    //  KNOCKBACK
    // ================================================================

    public void ApplyKnockback(Vector3 impulse)
    {
        depenTimer = DepenWindowAfterKnockback;

        // Dispatch to every IKnockbackReceiver (patrols, shooters, wall jumpers…)
        bool handled = false;
        if (receivers != null)
        {
            foreach (var r in receivers)
            {
                if (r == null) continue;
                r.ReceiveKnockback(impulse, knockbackDuration);
                handled = true;
            }
        }

        if (!handled)
            StartCoroutine(FallbackKnockback(impulse));
    }

    /// <summary>
    /// Fallback for enemies with no movement scripts (e.g. training dummy):
    /// ease-out the impulse directly on transform, with wall bounce.
    /// </summary>
    private IEnumerator FallbackKnockback(Vector3 impulse)
    {
        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / knockbackDuration;
            Vector3 step = impulse * (1f - t) * Time.deltaTime;
            step.z = 0f;

            if (wallLayers != 0)
            {
                step = KnockbackBouncer.StepWithWallBounce(
                    step, transform.position, wallCheckHalfExtents,
                    bounceDamping, wallLayers, selfColliders);
            }

            Vector3 newPos = transform.position + step;
            newPos.z = 0f;
            transform.position = newPos;
            yield return null;
        }
    }

    // ================================================================
    //  WALL DEPENETRATION — windowed, not every frame
    // ================================================================

    private void FixedUpdate()
    {
        if (wallLayers == 0) return;

        if (!alwaysDepenetrate)
        {
            if (depenTimer <= 0f) return;
            depenTimer -= Time.fixedDeltaTime;
        }

        Depenetrate();
    }

    private void Depenetrate()
    {
        int count = Physics.OverlapBoxNonAlloc(
            transform.position, wallCheckHalfExtents, depenBuffer,
            Quaternion.identity, wallLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider wallCol = depenBuffer[i];

            // Skip our own colliders
            bool isSelf = false;
            if (selfColliders != null)
                foreach (var sc in selfColliders)
                    if (sc == wallCol) { isSelf = true; break; }
            if (isSelf) continue;

            Vector3 closestOnWall = wallCol.ClosestPoint(transform.position);
            closestOnWall.z = 0f;
            Vector3 meFlat = new Vector3(transform.position.x, transform.position.y, 0f);
            float overlap = (closestOnWall - meFlat).magnitude;

            // Only push if closest point is very near (means we're overlapping)
            if (overlap < wallCheckHalfExtents.x * 1.1f)
            {
                Vector3 pushDir = (meFlat - closestOnWall).normalized;
                if (pushDir.sqrMagnitude < 0.001f) pushDir = Vector3.up;
                float pushAmount = wallCheckHalfExtents.x - overlap + 0.05f;
                if (pushAmount > 0f)
                {
                    Vector3 newPos = transform.position + pushDir * pushAmount;
                    newPos.z = 0f;
                    transform.position = newPos;
                }
            }
        }
    }
}
