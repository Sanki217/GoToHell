using System;

/// <summary>Start the run with an extra life (same one the shop sells).</summary>
[Serializable]
public class ExtraLifeEffect : RunEffect
{
    public override void Apply(in RunEffectContext ctx)
    {
        if (ctx.isRunStart) PlayerHealth.HasExtraLife = true;
    }

    public override string Describe(int stacks) => "Start with an extra life";
}
