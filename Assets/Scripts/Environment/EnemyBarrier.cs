using UnityEngine;

/// <summary>
/// Invisible line no enemy can cross. Place one at the top and one at the
/// bottom of a level, set the mode, done — no colliders or layers involved.
///
/// Works by clamping every registered enemy's Y position each physics step
/// (EnemyRegistry), so it catches ALL enemy movement styles: rigidbody,
/// transform-driven patrols, knockback, and future flying enemies.
/// The player, arrows, and souls are unaffected.
/// </summary>
public class EnemyBarrier : MonoBehaviour
{
    public enum BarrierMode
    {
        Top,     // enemies cannot go ABOVE this line
        Bottom   // enemies cannot go BELOW this line
    }

    [Header("Barrier")]
    public BarrierMode mode = BarrierMode.Top;

    [Tooltip("Extra distance enemies are kept away from the line.")]
    public float padding = 0f;

    private void FixedUpdate()
    {
        float lineY = transform.position.y;
        var enemies = EnemyRegistry.All;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null) continue;

            Vector3 pos = enemy.transform.position;

            if (mode == BarrierMode.Top && pos.y > lineY - padding)
                enemy.transform.position = new Vector3(pos.x, lineY - padding, pos.z);
            else if (mode == BarrierMode.Bottom && pos.y < lineY + padding)
                enemy.transform.position = new Vector3(pos.x, lineY + padding, pos.z);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = mode == BarrierMode.Top
            ? new Color(1f, 0.4f, 0.2f, 0.8f)
            : new Color(0.2f, 0.6f, 1f, 0.8f);
        Vector3 p = transform.position;
        Gizmos.DrawLine(p + Vector3.left * 200f, p + Vector3.right * 200f);
        Vector3 tick = mode == BarrierMode.Top ? Vector3.down : Vector3.up;
        for (int x = -200; x <= 200; x += 10)
            Gizmos.DrawLine(p + new Vector3(x, 0f, 0f), p + new Vector3(x, 0f, 0f) + tick * 1.5f);
    }
}
