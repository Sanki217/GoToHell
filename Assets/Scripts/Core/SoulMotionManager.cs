using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ticks the motion of every live Soul in ONE FixedUpdate instead of a
/// coroutine per soul (killstreak dumps used to mean dozens of concurrent
/// coroutines). Souls register in OnEnable / unregister in OnDisable.
///
/// Scene-scoped: auto-created on first Register, dies with the scene.
/// </summary>
public class SoulMotionManager : MonoBehaviour
{
    private static SoulMotionManager instance;

    private readonly List<Soul> souls = new List<Soul>(128);

    public static void Register(Soul soul)
    {
        if (soul == null) return;
        if (instance == null)
            instance = new GameObject("[SoulMotion]").AddComponent<SoulMotionManager>();
        if (!instance.souls.Contains(soul))
            instance.souls.Add(soul);
    }

    public static void Unregister(Soul soul)
    {
        if (instance != null && soul != null)
            instance.souls.Remove(soul);
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        for (int i = souls.Count - 1; i >= 0; i--)
        {
            Soul s = souls[i];
            if (s == null || !s.isActiveAndEnabled) { souls.RemoveAt(i); continue; }
            s.Tick(dt);
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
