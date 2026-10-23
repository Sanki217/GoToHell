using System;
using UnityEngine;

/// <summary>Heals the player whenever souls are collected. Priest passive.</summary>
[Serializable]
public class HealOnSoulEffect : RunEffect
{
    [Tooltip("HP restored per soul collected.")]
    public int hpPerSoul = 1;

    public override void Apply(in RunEffectContext ctx)
    {
        if (ctx.player == null) return;
        // State + event subscription live on a component (effects are shared assets)
        HealOnSoulPassive comp = ctx.player.GetComponent<HealOnSoulPassive>();
        if (comp == null) comp = ctx.player.AddComponent<HealOnSoulPassive>();
        comp.hpPerSoul += hpPerSoul * ctx.stacks;
    }

    public override string Describe(int stacks) => $"Heal {hpPerSoul * stacks} HP per soul collected";
}
