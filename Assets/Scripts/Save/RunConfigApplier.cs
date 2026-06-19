using UnityEngine;

/// <summary>
/// Applies the current RunConfig to the player when a gameplay scene loads.
/// Put this on the player (or a scene bootstrap object) in gameplay scenes.
///
/// Reads the stat allocation chosen in the Character Creator and adds it to
/// PlayerStats. If no RunConfig exists (e.g. testing the gameplay scene directly),
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

        // Refill HP to the new max after Health allocation
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null) health.SetHP(health.maxHP);
    }
}
