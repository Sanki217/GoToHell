using System;

/// <summary>
/// Build-identity tags for upgrades. An upgrade can have several
/// (Soul Arrow = Arrow | Soul | OnKill).
///
/// WHAT USES THEM
///   • PlayerUpgradePool — "build focus": an upgrade sharing tags with what
///     you already own is a bit more likely to be offered (synergy weight),
///     and pacts / meta upgrades can favour or ban tags (RunModifiers).
///   • Upgrade Balance Sheet (Tools → Go To Hell → Balance Sheet) — filter
///     and count by tag to spot holes ("only 1 Frost upgrade").
///   • Future: tag-based pacts ("Fire upgrades +20% damage"), achievements,
///     collection filters.
///
/// RULES
///   • Values are bit flags and are SAVED in prefabs as an int — never
///     reorder or renumber existing entries; only append new ones.
///   • Tag what the upgrade is ABOUT, not every system it touches.
/// </summary>
[Flags]
public enum UpgradeTag
{
    None       = 0,

    // Weapon / verb
    Arrow      = 1 << 0,
    Melee      = 1 << 1,
    Dash       = 1 << 2,
    Movement   = 1 << 3,

    // Elements
    Fire       = 1 << 4,
    Frost      = 1 << 5,
    Holy       = 1 << 6,
    Shock      = 1 << 7,

    // Hell economy (souls, blood, curses)
    Soul       = 1 << 8,
    Blood      = 1 << 9,
    Curse      = 1 << 10,

    // Effect shape
    Crit       = 1 << 11,
    Explosion  = 1 << 12,
    Projectile = 1 << 13,
    OnKill     = 1 << 14,
    Defense    = 1 << 15,
    Sustain    = 1 << 16,
    Control    = 1 << 17,
    Economy    = 1 << 18,
}

public static class UpgradeTagUtil
{
    /// <summary>Number of set bits (how many tags).</summary>
    public static int Count(UpgradeTag tags)
    {
        int v = (int)tags, c = 0;
        while (v != 0) { v &= v - 1; c++; }
        return c;
    }

    public static bool HasAny(this UpgradeTag tags, UpgradeTag other) => (tags & other) != 0;
}
