using UnityEngine;

/// <summary>
/// Attach to the SlashCollider child GameObject.
/// Uses Physics.OverlapBox each time it activates — works on both trigger
/// and non-trigger colliders on targets (two triggers don't fire OnTriggerEnter).
///
/// SETUP:
///   - Add to the SlashCollider child alongside a BoxCollider (Is Trigger = true)
///   - Parent must have PlayerSlash.cs
/// </summary>
public class SlashDamageCollider : MonoBehaviour
{
    private PlayerSlash playerSlash;
    private BoxCollider boxCollider;

    // Track hit objects this activation so we don't hit the same thing twice
    private System.Collections.Generic.HashSet<GameObject> hitThisSwing
        = new System.Collections.Generic.HashSet<GameObject>();

    private void Awake()
    {
        playerSlash = GetComponentInParent<PlayerSlash>();
        boxCollider = GetComponent<BoxCollider>();
    }

    private void OnEnable()
    {
        hitThisSwing.Clear();
        CheckOverlaps();
    }

    // Also check every frame while active (for the active duration window)
    private void Update()
    {
        CheckOverlaps();
    }

    private void CheckOverlaps()
    {
        if (playerSlash == null || boxCollider == null) return;

        // Build OverlapBox params from this collider's world bounds
        Vector3 center = transform.TransformPoint(boxCollider.center);
        Vector3 halfExt = Vector3.Scale(boxCollider.size * 0.5f, transform.lossyScale);
        // Use absolute values — prevents "negative size" warning when scale flips
        halfExt = new Vector3(Mathf.Abs(halfExt.x), Mathf.Abs(halfExt.y), Mathf.Abs(halfExt.z));
        Quaternion rotation = transform.rotation;

        Collider[] hits = Physics.OverlapBox(center, halfExt, rotation,
            Physics.AllLayers, QueryTriggerInteraction.Collide);

        foreach (var hit in hits)
        {
            if (hit == null || hit.gameObject == null) continue;

            // Don't hit ourselves or the player
            if (hit.transform.IsChildOf(playerSlash.transform)) continue;

            GameObject go = hit.gameObject;
            if (hitThisSwing.Contains(go)) continue;
            hitThisSwing.Add(go);

            if (go.CompareTag("Enemy"))
            {
                playerSlash.OnSlashHitEnemy(go);
                continue;
            }

            // Destructibles — check component anywhere on the object or its root
            Vase vase = go.GetComponentInParent<Vase>()
                     ?? go.GetComponent<Vase>();
            if (vase != null && !hitThisSwing.Contains(vase.gameObject))
            {
                hitThisSwing.Add(vase.gameObject);
                playerSlash.OnSlashHitDestructible(vase.gameObject);
                continue;
            }

            ExplosiveBarrel barrel = go.GetComponentInParent<ExplosiveBarrel>()
                                  ?? go.GetComponent<ExplosiveBarrel>();
            if (barrel != null && !hitThisSwing.Contains(barrel.gameObject))
            {
                hitThisSwing.Add(barrel.gameObject);
                playerSlash.OnSlashHitDestructible(barrel.gameObject);
            }
        }
    }
}