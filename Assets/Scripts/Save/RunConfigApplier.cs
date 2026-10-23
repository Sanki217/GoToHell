using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies the current RunConfig to the player when a scene loads (town and
/// every run layer). Lives on every player prefab.
///
/// 1. Character stats (1 + creation points) → PlayerStats.
/// 2. Kit — every kit component starts OFF, then this loadout goes ON:
///      LMB        the equipped weapon's components
///      Shift+LMB  PlayerKick (universal)
///      F          the class's active skill (ClassSkill)
///      RMB        the run's pact skill (not in the town)
///    Components are found by type name, so a new weapon / class / pact is
///    data, not code here.
/// 3. Class passive + weapon smith levels (RunEffects).
/// 4. RunModifiers — owned meta upgrades (runs only, not the town).
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
        bool hasCharacter = cfg != null && !string.IsNullOrEmpty(cfg.characterId);
        bool isRunStart = cfg == null || (!cfg.inTown && cfg.currentLayer <= 1);

        if (hasCharacter)
        {
            PlayerStats stats = GetComponent<PlayerStats>();
            stats.AddPrimary(PrimaryStat.Agility,      cfg.agility);
            stats.AddPrimary(PrimaryStat.AttackDamage, cfg.attackDamage);
            stats.AddPrimary(PrimaryStat.Luck,         cfg.luck);
            stats.AddPrimary(PrimaryStat.Psyche,       cfg.psyche);
            stats.AddPrimary(PrimaryStat.Health,       cfg.health);
            stats.AddPrimary(PrimaryStat.Size,         cfg.size);
            stats.AddPrimary(PrimaryStat.Cooldown,     cfg.cooldown);

            ClassDefinition cls = Content.Classes.Get(cfg.selectedClassId);
            WeaponDefinition weapon = Content.Weapons.Get(cfg.selectedWeaponId);
            PactDefinition pact = cfg.inTown ? null : Content.Pacts.Get(cfg.selectedPactId);

            ApplyKit(cls, weapon, pact);
            if (cls != null) cls.ApplyPassive(gameObject, isRunStart);
            if (weapon != null) weapon.ApplyLevels(gameObject, cfg.weaponLevel, isRunStart);
        }

        // Meta upgrades — runs only. One-time bonuses (starting souls, extra life) on Layer 1 only.
        if (cfg == null || !cfg.inTown)
            RunModifiers.ApplyToPlayer(gameObject, isRunStart);

        // Refill HP to the new max after Health allocation
        if (TryGetComponent(out PlayerHealth health)) health.SetHP(health.MaxHP);
    }

    private void ApplyKit(ClassDefinition cls, WeaponDefinition weapon, PactDefinition pact)
    {
        // Everything any weapon, class or pact could own starts OFF…
        var all = new HashSet<string>();
        foreach (WeaponDefinition w in Content.Weapons.All)
            if (w.weaponComponentNames != null) all.UnionWith(w.weaponComponentNames);
        foreach (ClassDefinition c in Content.Classes.All)
            if (!string.IsNullOrEmpty(c.activeSkillComponentName)) all.Add(c.activeSkillComponentName);
        foreach (PactDefinition p in Content.Pacts.All)
            if (!string.IsNullOrEmpty(p.skillComponentName)) all.Add(p.skillComponentName);
        foreach (string n in all) SetEnabled(n, false);

        // …then this loadout goes ON.
        if (weapon != null && weapon.weaponComponentNames != null)
            foreach (string n in weapon.weaponComponentNames) Require(n, $"weapon '{weapon.weaponId}'");
        else Debug.LogWarning("[RunConfigApplier] No weapon equipped — LMB does nothing.");

        if (cls != null && !string.IsNullOrEmpty(cls.activeSkillComponentName))
            Require(cls.activeSkillComponentName, $"class '{cls.classId}'");

        if (pact != null && !string.IsNullOrEmpty(pact.skillComponentName))
            Require(pact.skillComponentName, $"pact '{pact.pactId}'");

        // Shift+LMB kick is universal
        if (TryGetComponent(out PlayerKick kick)) kick.enabled = true;
        else Debug.LogWarning("[RunConfigApplier] Player prefab has no PlayerKick — run Tools → Go To Hell → " +
                              "Characters → Sync Kit Into Class Prefabs.");
    }

    private void Require(string componentName, string owner)
    {
        if (GetComponent(componentName) == null)
            Debug.LogError($"[RunConfigApplier] {owner} needs component '{componentName}' on the player prefab. " +
                           "Run Tools → Go To Hell → Characters → Sync Kit Into Class Prefabs.");
        SetEnabled(componentName, true);
    }

    private void SetEnabled(string componentName, bool on)
    {
        if (string.IsNullOrEmpty(componentName)) return;
        if (GetComponent(componentName) is Behaviour b) b.enabled = on;
    }
}
