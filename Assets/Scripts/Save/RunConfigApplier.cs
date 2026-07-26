using UnityEngine;

/// <summary>
/// Applies the current RunConfig to the player when a gameplay scene loads.
/// Lives on every player prefab.
///
/// 1. Adds the run's stat preset to PlayerStats.
/// 2. Enables every weapon component on the player — there is no weapon
///    choice anymore: bow on LMB, sword on Shift+LMB or empty quiver
///    (PlayerSlash legacy mode). The class RMB skill lives on the prefab.
///
/// If no RunConfig exists (e.g. testing the gameplay scene directly),
/// it does nothing and prefab defaults apply.
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

        EnableAllWeaponComponents();

        // Refill HP to the new max after Health allocation
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null) health.SetHP(health.maxHP);
    }

    /// <summary>
    /// Enables every weapon component listed by any WeaponDefinition, so the
    /// full kit works regardless of how the prefab was saved.
    /// </summary>
    private void EnableAllWeaponComponents()
    {
        WeaponDefinition[] defs = Resources.LoadAll<WeaponDefinition>("Weapons");
        if (defs == null) return;

        foreach (WeaponDefinition d in defs)
        {
            if (d == null || d.weaponComponentNames == null) continue;
            foreach (string n in d.weaponComponentNames)
            {
                if (string.IsNullOrEmpty(n)) continue;
                if (GetComponent(n) is Behaviour comp) comp.enabled = true;
            }
        }
    }
}
