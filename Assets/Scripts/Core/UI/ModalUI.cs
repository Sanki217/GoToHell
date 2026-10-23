using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Counts open modal windows (town station panels…). The pause menu ignores
/// Esc while one is open, so Esc closes the window instead of pausing over it.
/// </summary>
public static class ModalUI
{
    private static readonly HashSet<object> open = new HashSet<object>();

    public static bool AnyOpen
    {
        get
        {
            open.RemoveWhere(o => o is Object uo && uo == null);
            return open.Count > 0;
        }
    }

    public static void Push(object owner) { if (owner != null) open.Add(owner); }
    public static void Pop(object owner)  { if (owner != null) open.Remove(owner); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics() => open.Clear();
}
