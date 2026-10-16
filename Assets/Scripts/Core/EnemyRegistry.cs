using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Live registry of all active enemies. Enemies self-register in
/// OnEnable/OnDisable — consumers (SceneOptimizer, AoE upgrades, auto-aim,
/// bosses) iterate this list instead of scanning the scene with
/// FindGameObjectsWithTag / FindObjectsByType, which are O(scene).
///
/// Register/Unregister are O(1) (HashSet guard + swap-remove), so large
/// waves don't pay a linear cost per spawn/death.
/// </summary>
public static class EnemyRegistry
{
    private static readonly List<Enemy> enemies = new List<Enemy>(64);
    private static readonly Dictionary<Enemy, int> indexOf = new Dictionary<Enemy, int>(64);

    /// <summary>All currently active enemies. Do not cache across frames; do not modify. Order is not stable.</summary>
    public static IReadOnlyList<Enemy> All => enemies;

    public static int Count => enemies.Count;

    public static void Register(Enemy enemy)
    {
        if (enemy == null || indexOf.ContainsKey(enemy)) return;
        indexOf[enemy] = enemies.Count;
        enemies.Add(enemy);
    }

    public static void Unregister(Enemy enemy)
    {
        if (ReferenceEquals(enemy, null) || !indexOf.TryGetValue(enemy, out int i)) return;

        int last = enemies.Count - 1;
        Enemy moved = enemies[last];
        enemies[i] = moved;
        indexOf[moved] = i;
        enemies.RemoveAt(last);
        indexOf.Remove(enemy);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics()
    {
        enemies.Clear();
        indexOf.Clear();
    }
}
