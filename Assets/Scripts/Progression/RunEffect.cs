using System;
using UnityEngine;

/// <summary>
/// Custom logic attached to a meta upgrade — the escape hatch for
/// anything RunModifierSet's numbers can't express.
///
/// One class per effect, one file per class (same rule as upgrades). Effects
/// are plain serializable classes stored INSIDE the meta-upgrade asset with
/// [SerializeReference, SubclassSelector], so the Inspector shows a type
/// dropdown and the effect's own fields. No extra assets, no prefabs.
///
///   [Serializable]
///   public class StartWithShieldEffect : RunEffect
///   {
///       public int blocks = 1;
///       public override void Apply(in RunEffectContext ctx) { … }
///       public override string Describe(int stacks) => $"Start with {blocks * stacks} shield";
///   }
///
/// IMPORTANT: an effect instance lives in a ScriptableObject and is SHARED by
/// every run — never store per-run state in its fields. If the effect needs
/// state or event subscriptions, add a component to ctx.player in Apply and
/// keep the state there (it dies with the player).
/// </summary>
[Serializable]
public abstract class RunEffect
{
    /// <summary>Called when the player spawns in a gameplay scene (every layer).</summary>
    public abstract void Apply(in RunEffectContext ctx);

    /// <summary>Short description for menus and the balance sheet.</summary>
    public virtual string Describe(int stacks) => GetType().Name;
}

/// <summary>What a RunEffect gets when it is applied.</summary>
public readonly struct RunEffectContext
{
    /// <summary>The freshly spawned player.</summary>
    public readonly GameObject player;
    /// <summary>Meta upgrade rank.</summary>
    public readonly int stacks;
    /// <summary>True on the first layer of a run (one-time bonuses go here).</summary>
    public readonly bool isRunStart;

    public RunEffectContext(GameObject player, int stacks, bool isRunStart)
    {
        this.player = player;
        this.stacks = stacks;
        this.isRunStart = isRunStart;
    }
}

/// <summary>
/// Put on a [SerializeReference] field or list to get a type picker in the
/// Inspector (drawer: Editor/SubclassSelectorDrawer.cs).
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class SubclassSelectorAttribute : PropertyAttribute { }
