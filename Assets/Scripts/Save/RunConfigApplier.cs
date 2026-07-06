using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies the current RunConfig to the player when a gameplay scene loads.
/// Lives on every class player prefab.
///
/// 1. Adds the selected class's stat preset to PlayerStats.
/// 2. Enables the selected weapon's components and disables every other
///    weapon's (matched by type name via WeaponDefinition.weaponComponentNames).
///
/// The class RMB skill needs no toggling anymore — each class has its own
/// player prefab carrying only its skill components (see PlayerSpawner).
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

        ApplyWeaponComponents(cfg.selectedWeaponId);

        // Refill HP to the new max after Health allocation
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null) health.SetHP(health.maxHP);
    }

    /// <summary>
    /// Enables only the selected weapon's components on the player.
    /// </summary>
    private void ApplyWeaponComponents(string weaponId)
    {
        if (string.IsNullOrEmpty(weaponId)) return;   // no weapon chosen — leave prefab defaults

        WeaponDefinition[] defs = Resources.LoadAll<WeaponDefinition>("Weapons");
        if (defs == null || defs.Length == 0) return;

        WeaponDefinition selected = null;
        foreach (WeaponDefinition d in defs)
            if (d != null && d.weaponId == weaponId) { selected = d; break; }

        if (selected == null)
        {
            Debug.LogWarning($"[RunConfigApplier] No WeaponDefinition found for '{weaponId}' in Resources/Weapons.");
            return;
        }

        var selectedSet = new HashSet<string>();
        if (selected.weaponComponentNames != null)
            foreach (string n in selected.weaponComponentNames)
                if (!string.IsNullOrEmpty(n)) selectedSet.Add(n);

        // Union of every weapon's components — anything not in the selected set gets disabled
        var allNames = new HashSet<string>();
        foreach (WeaponDefinition d in defs)
        {
            if (d == null || d.weaponComponentNames == null) continue;
            foreach (string n in d.weaponComponentNames)
                if (!string.IsNullOrEmpty(n)) allNames.Add(n);
        }

        foreach (string name in allNames)
        {
            if (GetComponent(name) is Behaviour comp)
                comp.enabled = selectedSet.Contains(name);
            else if (selectedSet.Contains(name))
                Debug.LogWarning($"[RunConfigApplier] Player has no '{name}' component for weapon '{weaponId}'.");
        }
    }
}
