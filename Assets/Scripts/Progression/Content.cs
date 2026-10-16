using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cached lookup of one kind of definition asset loaded from a Resources
/// folder, by stable string id. Loads lazily, once per play session.
/// </summary>
public sealed class DefinitionCache<T> where T : ScriptableObject
{
    private readonly string folder;
    private readonly Func<T, string> idOf;
    private readonly Comparison<T> order;
    private List<T> all;
    private Dictionary<string, T> byId;

    public DefinitionCache(string resourcesFolder, Func<T, string> idSelector, Comparison<T> sortOrder = null)
    {
        folder = resourcesFolder;
        idOf = idSelector;
        order = sortOrder;
    }

    public IReadOnlyList<T> All
    {
        get { EnsureLoaded(); return all; }
    }

    /// <summary>The definition with this id, or null.</summary>
    public T Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        EnsureLoaded();
        return byId.TryGetValue(id, out T def) ? def : null;
    }

    public void Invalidate()
    {
        all = null;
        byId = null;
    }

    private void EnsureLoaded()
    {
        if (all != null) return;
        all = new List<T>();
        byId = new Dictionary<string, T>();

        foreach (T def in Resources.LoadAll<T>(folder))
        {
            if (def == null) continue;
            string id = idOf(def);
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[Content] {typeof(T).Name} '{def.name}' has no id — ignored.", def);
                continue;
            }
            if (byId.ContainsKey(id))
            {
                Debug.LogError($"[Content] Duplicate {typeof(T).Name} id '{id}' ('{def.name}' and " +
                               $"'{byId[id].name}'). Ids must be unique — save data uses them.", def);
                continue;
            }
            byId.Add(id, def);
            all.Add(def);
        }
        if (order != null) all.Sort(order);
    }
}

/// <summary>
/// Every data-driven content type, in one place. Assets live in
/// Assets/Resources/&lt;Folder&gt;/ and are found automatically — adding a pact or
/// a meta upgrade is "create the asset", nothing else.
/// </summary>
public static class Content
{
    public static readonly DefinitionCache<PactDefinition> Pacts =
        new DefinitionCache<PactDefinition>("Pacts", d => d.pactId,
            (a, b) => a.heat != b.heat ? a.heat.CompareTo(b.heat) : string.CompareOrdinal(a.displayName, b.displayName));

    public static readonly DefinitionCache<MetaUpgradeDefinition> MetaUpgrades =
        new DefinitionCache<MetaUpgradeDefinition>("Meta", d => d.metaId,
            (a, b) => a.sortOrder != b.sortOrder ? a.sortOrder.CompareTo(b.sortOrder) : string.CompareOrdinal(a.displayName, b.displayName));

    public static readonly DefinitionCache<ClassDefinition> Classes =
        new DefinitionCache<ClassDefinition>("Classes", d => d.classId);

    public static readonly DefinitionCache<WeaponDefinition> Weapons =
        new DefinitionCache<WeaponDefinition>("Weapons", d => d.weaponId);

    public static void InvalidateAll()
    {
        Pacts.Invalidate();
        MetaUpgrades.Invalidate();
        Classes.Invalidate();
        Weapons.Invalidate();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics() => InvalidateAll();
}
