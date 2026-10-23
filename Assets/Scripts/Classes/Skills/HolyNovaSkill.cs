using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Priest active — HOLY NOVA: a burst of light that heals the Priest and
/// smites (damages + applies Holy) every enemy around. Healing and damage
/// grow with Psyche.
/// </summary>
public class HolyNovaSkill : ClassSkill
{
    [Header("Holy Nova")]
    public float radius = 5f;
    [Tooltip("Heal as a fraction of max HP (0.15 = 15%).")]
    public float healFraction = 0.15f;
    public float baseDamage = 4f;
    public float damagePerPsyche = 0.4f;
    public bool applyHoly = true;

    private readonly List<Enemy> enemies = new List<Enemy>();

    private void Reset()
    {
        skillName = "Holy Nova";
        baseCooldown = 14f;
        effectColor = new Color(1f, 0.9f, 0.45f, 0.4f);
    }

    protected override bool Activate()
    {
        Vector3 c = transform.position;
        float r = Scaled(radius);

        if (health != null)
            health.RestoreHP(Mathf.Max(1, Mathf.RoundToInt(health.MaxHP * healFraction * potencyMultiplier)));

        float psyche = stats != null ? stats.psyche : 0f;
        int dmg = Mathf.Max(1, Mathf.RoundToInt((baseDamage + damagePerPsyche * psyche) * potencyMultiplier));
        CombatQuery.OverlapEnemies(c, r, enemies, combatantsOnly: true);
        foreach (Enemy e in enemies)
        {
            e.TakeDamage(dmg, c, Vector3.zero, 0f, false, FloatingTextManager.HitType.HolyDetonate);
            if (stats != null) stats.RecordDamageDealt(dmg, DamageSource.Status, e.gameObject);
            if (applyHoly && !e.IsDead) e.ApplyStatus(StatusType.Holy, stats, upgrades);
        }

        DebugShapes.Sphere(c, r, effectColor, 0.35f);
        return true;
    }

    protected override float DebugRadius => radius;

    public override string Describe() =>
        $"Heal {healFraction * 100f:0}% max HP and smite every enemy within {radius:0.#} m with Holy light.";
}
