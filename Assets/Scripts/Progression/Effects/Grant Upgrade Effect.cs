using System;
using UnityEngine;

/// <summary>
/// Starts the player with an upgrade already owned — e.g. "Pact of Embers:
/// begin every run with Burning Weapon". Uses the normal upgrade orb prefab
/// from the pool, so the upgrade behaves exactly as if it had been picked.
///
/// Applied on every layer (the player is rebuilt per scene); skipped if the
/// player already owns it.
/// </summary>
[Serializable]
public class GrantUpgradeEffect : RunEffect
{
    [Tooltip("An upgrade orb prefab (same ones listed in the UpgradePool).")]
    public GameObject upgradePrefab;
    public UpgradeRarity rarity = UpgradeRarity.Common;

    public override void Apply(in RunEffectContext ctx)
    {
        if (upgradePrefab == null || ctx.player == null) return;
        if (!ctx.player.TryGetComponent(out PlayerUpgradeManager mgr)) return;
        mgr.GrantUpgradeFromPrefab(upgradePrefab, rarity);
    }

    public override string Describe(int stacks)
    {
        string name = "an upgrade";
        if (upgradePrefab != null && upgradePrefab.TryGetComponent(out PlayerUpgrade u))
            name = string.IsNullOrEmpty(u.displayName) ? u.UpgradeId : u.displayName;
        return $"Start with {name}";
    }
}
