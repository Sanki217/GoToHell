using UnityEngine;

/// <summary>
/// Applies the current RunConfig to the player when a gameplay scene loads.
/// Put this on the player in gameplay scenes.
///
/// 1. Adds the selected class's stat preset to PlayerStats.
/// 2. Enables the selected class's RMB skill component and disables the
///    skill components of every other class (all class-skill components
///    live on the player prefab, disabled or not — this picks the right one).
///
/// If no RunConfig exists (e.g. testing the gameplay scene directly),
/// it does nothing and the player uses Inspector defaults.
/// </summary>
[RequireComponent(typeof(PlayerStats))]
public class RunConfigApplier : MonoBehaviour
{
    private void Start()
    {
        if (RunConfig.I == null) return;   // testing scene directly — use defaults

        PlayerStats stats = GetComponent<PlayerStats>();
        RunConfig cfg = RunConfig.I;

        stats.AddPrimary(PrimaryStat.Agility,      cfg.agility);
        stats.AddPrimary(PrimaryStat.AttackDamage, cfg.attackDamage);
        stats.AddPrimary(PrimaryStat.Luck,         cfg.luck);
        stats.AddPrimary(PrimaryStat.Psyche,       cfg.psyche);
        stats.AddPrimary(PrimaryStat.Health,       cfg.health);
        stats.AddPrimary(PrimaryStat.Size,         cfg.size);
        stats.AddPrimary(PrimaryStat.Cooldown,     cfg.cooldown);

        ApplyClassSkill(cfg.selectedClassId);

        // Refill HP to the new max after Health allocation
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null) health.SetHP(health.maxHP);
    }

    /// <summary>
    /// Enables only the selected class's RMB skill component on the player.
    /// Skill components are matched by type name (ClassDefinition.skillComponentName).
    /// </summary>
    private void ApplyClassSkill(string classId)
    {
        if (string.IsNullOrEmpty(classId)) return;   // no class chosen — leave prefab defaults

        ClassDefinition[] defs = Resources.LoadAll<ClassDefinition>("Classes");
        if (defs == null || defs.Length == 0) return;

        string selectedSkill = null;
        foreach (ClassDefinition d in defs)
            if (d != null && d.classId == classId) { selectedSkill = d.skillComponentName; break; }

        if (string.IsNullOrEmpty(selectedSkill))
        {
            Debug.LogWarning($"[RunConfigApplier] No ClassDefinition found for '{classId}' in Resources/Classes.");
            return;
        }

        foreach (ClassDefinition d in defs)
        {
            if (d == null || string.IsNullOrEmpty(d.skillComponentName)) continue;
            if (GetComponent(d.skillComponentName) is Behaviour skill)
                skill.enabled = d.skillComponentName == selectedSkill;
            else if (d.skillComponentName == selectedSkill)
                Debug.LogWarning($"[RunConfigApplier] Player has no '{selectedSkill}' component for class '{classId}'.");
        }
    }
}
