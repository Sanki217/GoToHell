using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies the current RunConfig to the player when a gameplay scene loads.
/// Lives on every player prefab.
///
/// 1. Character stats (class base + creation points) → PlayerStats.
/// 2. Kit: the class's LMB weapon + Shift+LMB alt attack are enabled, every
///    other class's are disabled; the run's pact (RMB skill) is enabled and
///    every other pact skill disabled. Components are found by type name, so
///    a new class / pact is data, not code here.
/// 3. RunModifiers — owned meta upgrades (stats, max HP, starting souls, effects).
///
/// No RunConfig (testing a scene directly): prefab defaults are kept, meta
/// upgrades still apply.
/// </summary>
[RequireComponent(typeof(PlayerStats))]
public class RunConfigApplier : MonoBehaviour
{
    private void Start()
    {
        RunConfig cfg = RunConfig.I;

        if (cfg != null && !string.IsNullOrEmpty(cfg.characterId))
        {
            PlayerStats stats = GetComponent<PlayerStats>();
            stats.AddPrimary(PrimaryStat.Agility,      cfg.agility);
            stats.AddPrimary(PrimaryStat.AttackDamage, cfg.attackDamage);
            stats.AddPrimary(PrimaryStat.Luck,         cfg.luck);
            stats.AddPrimary(PrimaryStat.Psyche,       cfg.psyche);
            stats.AddPrimary(PrimaryStat.Health,       cfg.health);
            stats.AddPrimary(PrimaryStat.Size,         cfg.size);
            stats.AddPrimary(PrimaryStat.Cooldown,     cfg.cooldown);

            ApplyKit(Content.Classes.Get(cfg.selectedClassId), Content.Pacts.Get(cfg.selectedPactId));
        }

        // Meta upgrades. One-time bonuses (starting souls, extra life) on Layer 1 only.
        bool isRunStart = cfg == null || cfg.currentLayer <= 1;
        RunModifiers.ApplyToPlayer(gameObject, isRunStart);

        // Refill HP to the new max after Health allocation
        if (TryGetComponent(out PlayerHealth health)) health.SetHP(health.MaxHP);
    }

    private void ApplyKit(ClassDefinition cls, PactDefinition pact)
    {
        // Everything any class or pact could own starts OFF…
        var all = new HashSet<string>();
        foreach (ClassDefinition c in Content.Classes.All)
        {
            if (c.weapon != null && c.weapon.weaponComponentNames != null) all.UnionWith(c.weapon.weaponComponentNames);
            if (!string.IsNullOrEmpty(c.altAttackComponentName)) all.Add(c.altAttackComponentName);
        }
        foreach (PactDefinition p in Content.Pacts.All)
            if (!string.IsNullOrEmpty(p.skillComponentName)) all.Add(p.skillComponentName);
        foreach (string n in all) SetEnabled(n, false);

        // …then this run's kit goes ON.
        if (cls != null)
        {
            if (cls.weapon != null && cls.weapon.weaponComponentNames != null)
                foreach (string n in cls.weapon.weaponComponentNames)
                {
                    SetEnabled(n, true);
                    if (GetComponent(n) is IAltAttack a) a.SetAltAttackMode(false);
                }

            if (!string.IsNullOrEmpty(cls.altAttackComponentName))
            {
                SetEnabled(cls.altAttackComponentName, true);
                if (GetComponent(cls.altAttackComponentName) is IAltAttack alt) alt.SetAltAttackMode(true);
            }
        }
        else Debug.LogWarning("[RunConfigApplier] Unknown class — weapon kit not applied.");

        if (pact != null && !string.IsNullOrEmpty(pact.skillComponentName))
        {
            if (GetComponent(pact.skillComponentName) == null)
                Debug.LogError($"[RunConfigApplier] Pact '{pact.pactId}' needs component '{pact.skillComponentName}' on the " +
                               "player prefab. Run Tools → Go To Hell → Characters → Sync Pact Skills Into Class Prefabs.");
            SetEnabled(pact.skillComponentName, true);
        }
    }

    private void SetEnabled(string componentName, bool on)
    {
        if (string.IsNullOrEmpty(componentName)) return;
        if (GetComponent(componentName) is Behaviour b) b.enabled = on;
    }
}
