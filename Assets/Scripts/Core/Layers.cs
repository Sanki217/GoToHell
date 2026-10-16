using UnityEngine;

/// <summary>
/// Cached physics layer masks by name. LayerMask.GetMask/NameToLayer do a
/// string lookup (and GetMask allocates a params array), so per-frame calls
/// in knockback loops were pure garbage. Resolved lazily on first use — never
/// from a static initializer, which can run on Unity's loading thread.
///
/// Layer names must match Project Settings → Tags and Layers.
/// </summary>
public static class Layers
{
    private static int wall = -2, ground = -2, enemy = -2, pickup = -2, destructibles = -2;

    public static int Wall          => Resolve(ref wall, "Wall");
    public static int Ground        => Resolve(ref ground, "Ground");
    public static int Enemy         => Resolve(ref enemy, "Enemy");
    public static int Pickup        => Resolve(ref pickup, "Pickup");
    public static int Destructibles => Resolve(ref destructibles, "Destructibles");

    /// <summary>Wall + Ground — the solid level geometry.</summary>
    public static LayerMask SolidMask => MaskOf(Wall) | MaskOf(Ground);

    /// <summary>Enemy layer mask, or every layer if the Enemy layer doesn't exist.</summary>
    public static LayerMask EnemyMask => Enemy >= 0 ? MaskOf(Enemy) : Physics.AllLayers;

    public static int MaskOf(int layer) => layer >= 0 ? 1 << layer : 0;

    private static int Resolve(ref int cache, string name)
    {
        if (cache == -2) cache = LayerMask.NameToLayer(name);
        return cache;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics() => wall = ground = enemy = pickup = destructibles = -2;
}
