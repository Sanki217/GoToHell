using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rogue active — SMOKE BOMB: vanish in smoke. The Rogue can't be hurt while
/// it lasts, and enemies caught in the cloud are slowed. The next arrow fired
/// is a guaranteed crit.
/// </summary>
public class SmokeBombSkill : ClassSkill
{
    [Header("Smoke Bomb")]
    public float radius = 4f;
    [Tooltip("Seconds of invulnerability.")]
    public float duration = 1.5f;
    [Tooltip("Enemy speed inside the smoke (0.4 = 40% speed).")]
    [Range(0f, 1f)] public float slowSpeed = 0.4f;
    [Tooltip("Enemies stay slowed this long after the smoke clears.")]
    public float slowLinger = 1f;
    public bool nextArrowCrits = true;

    private readonly List<Enemy> enemies = new List<Enemy>();

    private void Reset()
    {
        skillName = "Smoke Bomb";
        baseCooldown = 12f;
        effectColor = new Color(0.6f, 0.6f, 0.65f, 0.45f);
    }

    protected override bool Activate()
    {
        Vector3 c = transform.position;
        float d = duration + durationBonus;
        if (health != null) health.StartDashInvincibility(d);
        if (nextArrowCrits && stats != null) stats.nextArrowForceCrit = true;

        object token = new object();
        CombatQuery.OverlapEnemies(c, Scaled(radius), enemies, combatantsOnly: true);
        foreach (Enemy e in enemies) e.SetSlow(token, slowSpeed);
        if (enemies.Count > 0) StartCoroutine(ClearSlow(new List<Enemy>(enemies), token, d + slowLinger));

        DebugShapes.Sphere(c, Scaled(radius), effectColor, d);
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
        $"Vanish: invulnerable for {duration:0.#} s, enemies in the smoke are slowed, next arrow crits.";
}
