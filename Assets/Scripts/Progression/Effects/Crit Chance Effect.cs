using System;
using UnityEngine;

/// <summary>Flat bonus crit chance (0.1 = +10%). Rogue passive.</summary>
[Serializable]
public class CritChanceEffect : RunEffect
{
    [Tooltip("Added crit chance. 0.1 = +10%.")]
    public float bonus = 0.1f;

    public override void Apply(in RunEffectContext ctx)
    {
        if (ctx.player != null && ctx.player.TryGetComponent(out PlayerStats stats))
            stats.AddBonusCritChance(bonus * ctx.stacks);
    }

    public override string Describe(int stacks) => $"+{bonus * stacks * 100f:0}% crit chance";
}
