using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Live registry of all active enemies. Enemies self-register in
/// OnEnable/OnDisable — consumers (SceneOptimizer, future AoE upgrades,
/// bosses) iterate this list instead of scanning the scene with
/// FindGameObjectsWithTag / FindObjectsByType, which are O(scene).
/// </summary>
public static class EnemyRegistry
{
    private static readonly List<Enemy> enemies = new List<Enemy>(64);

    /// <summary>All currently active enemies. Do not cache across frames; do not modify.</summary>
    public static IReadOnlyList<Enemy> All => enemies;

    public static int Count => enemies.Count;

    public static void Register(Enemy enemy)
    {
        if (enemy != null && !enemies.Contains(enemy))
            enemies.Add(enemy);
    }

    public static void Unregister(Enemy enemy)
    {
        enemies.Remove(enemy);
    }
}
