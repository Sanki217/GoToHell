using UnityEngine;

/// <summary>
/// Spawned by UpgradeGravityArrow when an arrow sticks to a surface.
///
/// Each FixedUpdate (runs AFTER enemy movement scripts via execution order 500)
/// this zone nudges every enemy within pullRadius toward its centre.
/// Because it runs last, the pull is applied on top of the enemy's own movement,
/// creating a real drag — the enemy drifts toward the arrow even while patrolling.
///
/// Multiple zones (arrows placed around an enemy) create competing pulls.
/// If the pulls cancel each other out, the enemy is effectively trapped.
/// </summary>
[DefaultExecutionOrder(500)]   // runs after enemy patrol/movement scripts in FixedUpdate
public class GravityArrowZone : MonoBehaviour
{
    [HideInInspector] public float pullRadius  = 4f;
    [HideInInspector] public float pullSpeed   = 2f;   // units per second at zone centre
    [HideInInspector] public float duration    = 3f;

    private float timer;

    private void Start()
    {
        timer = duration;
    }

    private void FixedUpdate()
    {
        timer -= Time.fixedDeltaTime;
        if (timer <= 0f) { Destroy(gameObject); return; }

        // Find all colliders within pull radius — check for Enemy component
        Collider[] hits = Physics.OverlapSphere(transform.position, pullRadius);
        foreach (Collider col in hits)
        {
            Enemy enemy = col.GetComponent<Enemy>()
                          ?? col.transform.root.GetComponent<Enemy>();
            if (enemy == null) continue;

            Vector3 toZone = transform.position - enemy.transform.position;
            toZone.z = 0f;
            float dist = toZone.magnitude;
            if (dist < 0.01f) continue;

            // Pull strength falls off linearly with distance
            float strength = Mathf.Clamp01(1f - dist / pullRadius);
            Vector3 pull = toZone.normalized * pullSpeed * strength * Time.fixedDeltaTime;

            enemy.transform.position = new Vector3(
                enemy.transform.position.x + pull.x,
                enemy.transform.position.y + pull.y,
                0f);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        float t = timer / Mathf.Max(duration, 0.001f);
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.15f + 0.25f * t);
        Gizmos.DrawSphere(transform.position, pullRadius);
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, pullRadius);
    }
#endif
}
