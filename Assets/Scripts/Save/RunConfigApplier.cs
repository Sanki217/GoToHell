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
/// 3. Applies RunModifiers — active pacts + owned meta upgrades (stats,
///    max HP, starting souls, custom RunEffects).
///
/// If no RunConfig exists (e.g. testing the gameplay scene directly), the
/// class preset is skipped but meta upgrades still apply, so balance testing
/// in a single scene matches a real run.
/// </summary>
[RequireComponent(typeof(PlayerStats))]
public class RunConfigApplier : MonoBehaviour
{
    private void Start()
    {
        RunConfig cfg = RunConfig.I;

        if (cfg != null)
        {
            PlayerStats stats = GetComponent<PlayerStats>();
            stats.AddPrimary(PrimaryStat.Agility,      cfg.agility);
            stats.AddPrimary(PrimaryStat.AttackDamage, cfg.attackDamage);
            stats.AddPrimary(PrimaryStat.Luck,         cfg.luck);
            stats.AddPrimary(PrimaryStat.Psyche,       cfg.psyche);
            stats.AddPrimary(PrimaryStat.Health,       cfg.health);
            stats.AddPrimary(PrimaryStat.Size,         cfg.size);
            stats.AddPrimary(PrimaryStat.Cooldown,     cfg.cooldown);

            EnableAllWeaponComponents();
        }

        // Pacts + meta upgrades. One-time bonuses (starting souls, extra life) on Layer 1 only.
        bool isRunStart = cfg == null || cfg.currentLayer <= 1;
        RunModifiers.ApplyToPlayer(gameObject, isRunStart);

        // Refill HP to the new max after Health allocation
        if (TryGetComponent(out PlayerHealth health)) health.SetHP(health.MaxHP);
    }

    /// <summary>
    /// Enables every weapon component listed by any WeaponDefinition, so the
    /// full kit works regardless of how the prefab was saved.
    /// </summary>
    private void EnableAllWeaponComponents()
    {
        foreach (WeaponDefinition d in Content.Weapons.All)
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
