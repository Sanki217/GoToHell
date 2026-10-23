using System;
using UnityEngine;

/// <summary>Arrows pass through extra enemies (weapon level).</summary>
[Serializable]
public class ArrowPierceEffect : RunEffect
{
    public int pierce = 1;

    public override void Apply(in RunEffectContext ctx)
    {
        if (ctx.player != null && ctx.player.TryGetComponent(out PlayerStats stats))
            stats.arrowPierceCount += pierce * ctx.stacks;
    }

    public override string Describe(int stacks) => $"Arrows pierce {pierce * stacks} more enem{(pierce * stacks == 1 ? "y" : "ies")}";
}
