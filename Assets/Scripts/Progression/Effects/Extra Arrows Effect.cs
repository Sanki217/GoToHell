using System;
using UnityEngine;

/// <summary>Bigger quiver (weapon level).</summary>
[Serializable]
public class ExtraArrowsEffect : RunEffect
{
    public int arrows = 1;

    public override void Apply(in RunEffectContext ctx)
    {
        if (ctx.player != null && ctx.player.TryGetComponent(out PlayerShooting shooting))
            shooting.AddMaxArrows(arrows * ctx.stacks);
    }

    public override string Describe(int stacks) => $"+{arrows * stacks} arrow{(arrows * stacks == 1 ? "" : "s")} in the quiver";
}
