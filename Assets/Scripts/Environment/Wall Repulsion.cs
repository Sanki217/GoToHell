using UnityEngine;

/// <summary>
/// Add this to your left and right side wall GameObjects.
/// Any enemy inside the trigger is pushed away from the wall.
/// The closer the enemy, the stronger the push.
///
/// Setup:
///   1. Select your Wall Left and Wall Right GameObjects
///   2. Add a BoxCollider (trigger) that extends slightly INWARD from the wall surface
///      (e.g. width 3-4 units into the play area)
///   3. Add this WallRepulsion script
///   4. Tune repulsionForce and maxRepulsionDistance in Inspector
/// </summary>
public class WallRepulsion : MonoBehaviour
{
    [Header("Repulsion Settings")]
    [Tooltip("Force applied to enemies at the wall surface (maximum push)")]
    public float repulsionForce = 20f;

    [Tooltip("Distance from wall at which repulsion starts to fade (falls off linearly)")]
    public float maxRepulsionDistance = 3f;

    [Tooltip("Direction to push — set to (1,0,0) for right wall, (-1,0,0) for left wall")]
    public Vector3 pushDirection = Vector3.right;

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;

        // Distance from this wall's surface to the enemy
        Vector3 wallPos = transform.position;
        Vector3 enemyPos = other.transform.position;

        float dist = Vector3.Dot(enemyPos - wallPos, pushDirection.normalized);
        dist = Mathf.Abs(dist);

        if (dist >= maxRepulsionDistance) return;

        // Stronger push the closer they are (linear falloff)
        float t = 1f - Mathf.Clamp01(dist / maxRepulsionDistance);
        float force = repulsionForce * t;

        // Apply as direct position offset — works with kinematic rigidbodies
        Vector3 push = pushDirection.normalized * force * Time.fixedDeltaTime;
        push.z = 0f;

        Transform enemy = other.transform.root;
        Vector3 newPos = enemy.position + push;
        newPos.z = enemy.position.z;
        enemy.position = newPos;
    }
}