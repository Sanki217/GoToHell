using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Universal Shift+LMB kick — every class, every weapon. A short, weak hit
/// toward the aim direction: breaks vases and barrels when the quiver is
/// empty, pokes enemies away. Damage scales a little with Attack Damage.
///
/// Weapons ignore LMB while Shift is held, so the kick never fires a weapon.
/// Added to every class prefab by Tools → Go To Hell → Characters → Sync Kit
/// Into Class Prefabs; RunConfigApplier enables it.
/// </summary>
public class PlayerKick : MonoBehaviour, IDebugDrawable
{
    [Header("Hit")]
    public float baseDamage = 2f;
    [Tooltip("Extra damage per Attack Damage point.")]
    public float damagePerAttack = 0.15f;
    [Tooltip("Reach in front of the player (world units).")]
    public float reach = 1.3f;
    [Tooltip("Hit box thickness.")]
    public float width = 0.9f;
    public float knockback = 6f;
    public float cooldown = 0.45f;

    [Header("Feedback")]
    public Color debugColor = new Color(1f, 0.9f, 0.4f, 0.5f);
    public bool showHitShape = true;

    private static readonly Collider[] buffer = new Collider[64];
    private readonly HashSet<int> hit = new HashSet<int>();
    private float readyAt;
    private PlayerStats stats;
    private PlayerStateController state;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        state = GetComponent<PlayerStateController>();
    }

    private void Update()
    {
        if (state != null && !state.HasControl()) return;
        if (!GameInput.AltAttackHeld || !GameInput.AttackPressed) return;
        if (Time.time < readyAt) return;
        Kick();
    }

    private float Damage => baseDamage + damagePerAttack * (stats != null ? stats.attackDamage : 0f);

    private void Kick()
    {
        readyAt = Time.time + cooldown;

        Vector3 dir = GameInput.AimDirectionFrom(transform.position, Vector3.right);
        Vector3 center = transform.position + dir * (reach * 0.5f);
        center.z = 0f;
        Quaternion rot = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        Vector3 half = new Vector3(reach * 0.5f, width * 0.5f, 0.6f);

        if (showHitShape) DebugShapes.Sphere(center, Mathf.Max(reach, width) * 0.5f, debugColor, 0.1f);

        hit.Clear();
        int count = Physics.OverlapBoxNonAlloc(center, half, buffer, rot, Physics.AllLayers, QueryTriggerInteraction.Collide);
        int dmg = Mathf.Max(1, Mathf.RoundToInt(Damage));
        for (int i = 0; i < count; i++)
        {
            Collider col = buffer[i];
            buffer[i] = null;
            if (col == null || col.transform.IsChildOf(transform)) continue;

            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                if (enemy.IsDead || !hit.Add(enemy.GetInstanceID())) continue;
                Vector3 kb = enemy.transform.position - transform.position;
                kb.z = 0f;
                if (kb.sqrMagnitude < 0.001f) kb = dir;
                enemy.TakeDamage(dmg, transform.position, kb.normalized, knockback, false,
                                 FloatingTextManager.HitType.Normal);
                if (stats != null) stats.RecordDamageDealt(dmg, DamageSource.Slash, enemy.gameObject);
                continue;
            }

            IDestructible d = CombatQuery.DestructibleOf(col);
            if (d is Object uo && hit.Add(uo.GetInstanceID())) d.TakeDamage(dmg);
        }
    }

    public void DrawDebug()
    {
        Vector3 dir = GameInput.AimDirectionFrom(transform.position, Vector3.right);
        Vector3 center = transform.position + dir * (reach * 0.5f);
        Vector3 side = new Vector3(-dir.y, dir.x, 0f) * (width * 0.5f);
        Vector3 fwd = dir * (reach * 0.5f);
        Color c = DebugColors.Player;
        DebugDraw.Line(center - fwd - side, center + fwd - side, c);
        DebugDraw.Line(center + fwd - side, center + fwd + side, c);
        DebugDraw.Line(center + fwd + side, center - fwd + side, c);
        DebugDraw.Line(center - fwd + side, center - fwd - side, c);
        DebugDraw.Label(center + fwd, $"kick {Mathf.RoundToInt(Damage)}" + (Time.time < readyAt ? $" ({readyAt - Time.time:0.0}s)" : ""), c);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = debugColor;
        Gizmos.DrawWireCube(transform.position + Vector3.right * reach * 0.5f, new Vector3(reach, width, 0.2f));
    }
}
