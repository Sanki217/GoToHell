using UnityEngine;

/// <summary>
/// Central place for global physics-layer collision rules. Runs once at game
/// start, before any scene loads. Add new layer rules HERE, not in scattered
/// Physics.IgnoreCollision calls.
///
/// Current rules:
///   • Pickup layer (10: souls, arrows, orbs) does not collide with itself —
///     souls no longer bounce off stuck arrows or each other; they only
///     interact with walls/platforms (via collision) and the player's looter
///     (via overlap/trigger queries, which ignore this matrix).
/// </summary>
public static class PhysicsLayerSetup
{
    /// <summary>Layer shared by souls, arrows, and orb pickups (see prefabs).</summary>
    public const int PickupLayer = 10;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        Physics.IgnoreLayerCollision(PickupLayer, PickupLayer, true);

        Debug.Log($"[PhysicsLayerSetup] '{LayerMask.LayerToName(PickupLayer)}' (layer {PickupLayer}) " +
                  "no longer self-collides — souls ignore stuck arrows.");
    }
}
