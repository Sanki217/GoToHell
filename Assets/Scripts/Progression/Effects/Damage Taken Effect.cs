using System;
using UnityEngine;

/// <summary>Scales damage the player takes (0.85 = 15% less). Warrior passive.</summary>
[Serializable]
public class DamageTakenEffect : RunEffect
{
    [Tooltip("Multiplier on damage taken. 0.85 = take 15% less.")]
    public float multiplier = 0.85f;

    public override void Apply(in RunEffectContext ctx)
    {
        if (ctx.player != null && ctx.player.TryGetComponent(out PlayerHealth health))
            health.damageTakenMultiplier *= Mathf.Pow(multiplier, ctx.stacks);
    }

    public override string Describe(int stacks) =>
        $"Take {(1f - Mathf.Pow(multiplier, stacks)) * 100f:0}% less damage";
}
