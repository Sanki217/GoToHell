using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Warrior active — WAR CRY: a shout that hurts, knocks back and staggers
/// (slows) every enemy around the Warrior. Radius grows with Size, damage with
/// Attack Damage. Breaks nearby vases too.
/// </summary>
public class WarCrySkill : ClassSkill
{
    [Header("War Cry")]
    public float radius = 5f;
    public float baseDamage = 6f;
    public float damagePerAttack = 0.5f;
    public float knockback = 12f;
    [Tooltip("Enemy speed while staggered (0.2 = 20% speed).")]
    [Range(0f, 1f)] public float staggerSpeed = 0.2f;
    public float staggerDuration = 1.5f;

    private readonly List<Enemy> enemies = new List<Enemy>();
    private readonly List<IDestructible> props = new List<IDestructible>();

    private void Reset()
    {
        skillName = "War Cry";
        baseCooldown = 10f;
        effectColor = new Color(1f, 0.35f, 0.2f, 0.35f);
    }

    protected override bool Activate()
    {
        Vector3 c = transform.position;
        float r = Scaled(radius);
        int dmg = Mathf.Max(1, Mathf.RoundToInt((baseDamage + damagePerAttack * (stats != null ? stats.attackDamage : 0f)) * potencyMultiplier));
        float duration = staggerDuration + durationBonus;
        object token = new object();

        CombatQuery.OverlapEnemies(c, r, enemies, combatantsOnly: true);
        foreach (Enemy e in enemies)
        {
            Vector3 dir = e.transform.position - c;
            dir.z = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.up;
            e.TakeDamage(dmg, c, dir.normalized, knockback, false, FloatingTextManager.HitType.Normal);
            if (stats != null) stats.RecordDamageDealt(dmg, DamageSource.Explosion, e.gameObject);
            if (!e.IsDead) e.SetSlow(token, staggerSpeed);
        }
        if (enemies.Count > 0) StartCoroutine(ClearSlow(new List<Enemy>(enemies), token, duration));

        CombatQuery.OverlapDestructibles(c, r, props);
        foreach (IDestructible d in props) d.TakeDamage(dmg);

        DebugShapes.Sphere(c, r, effectColor, 0.3f);
        Shake(0.35f, 0.2f);
        return true;
    }

    private static IEnumerator ClearSlow(List<Enemy> list, object token, float after)
    {
        yield return new WaitForSeconds(after);
        foreach (Enemy e in list)
            if (e != null) e.ClearSlow(token);
    }

    protected override float DebugRadius => radius;

    public override string Describe() =>
        $"Shout: damage, knock back and stagger every enemy within {radius:0.#} m for {staggerDuration:0.#} s.";
}
