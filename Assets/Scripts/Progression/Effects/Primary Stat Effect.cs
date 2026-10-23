using System;
using UnityEngine;

/// <summary>Adds primary stat points (e.g. weapon level "+2 Attack Damage").</summary>
[Serializable]
public class PrimaryStatEffect : RunEffect
{
    public PrimaryStat stat = PrimaryStat.AttackDamage;
    public float amount = 1f;

    public override void Apply(in RunEffectContext ctx)
    {
        if (ctx.player != null && ctx.player.TryGetComponent(out PlayerStats stats))
            stats.AddPrimary(stat, amount * ctx.stacks);
    }

    public override string Describe(int stacks) =>
        $"+{amount * stacks:0.#} {CharacterCreateUI.StatName(stat)}";
}
