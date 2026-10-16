using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools → Go To Hell → Balance Sheet
///
/// Every balance number in the game on one screen, editable in place:
///   • Upgrades — rarity, tags, availability, stat counts and EVERY tuning
///     field of every upgrade prefab (expand a row). Rarity/tag counts and
///     warnings (untagged, not in pool, missing icon, duplicate names).
///     CSV export/import for spreadsheet balancing.
///   • Enemies  — every EnemyDefinition with a per-layer preview.
///   • Pacts    — RMB skill component, unlock state and source.
///   • Meta     — rank costs, total cost to max, and "how many runs to buy
///     everything" from a sample run's payout.
///
/// Edits write straight to the prefab/asset (Undo works). Press Save Assets
/// (or Ctrl+S) to flush to disk.
/// </summary>
public class BalanceSheetWindow : EditorWindow
{
    private enum Tab { Upgrades, Enemies, Pacts, Meta }

    private Tab tab;
    private Vector2 scroll;
    private string search = "";

    // Upgrades
    private bool showAllProjectUpgrades;
    private int rarityFilter = -1;   // −1 = all
    private UpgradeTag tagFilter;
    private readonly HashSet<string> expanded = new HashSet<string>();
    private List<UpgradeRow> upgradeRows;
    private string[] weaponIds = new string[0], pactIds = new string[0];

    // Enemies
    private int previewLayer = 1;

    // Meta economy preview
    private int sampleKills = 60, sampleLayer = 2, sampleLayersDone = 1, sampleLevel = 8, sampleMinutes = 12;
    private bool sampleVictory;

    private class UpgradeRow
    {
        public GameObject prefab;
        public PlayerUpgrade upgrade;
        public SerializedObject so;
        public bool inPool;
    }

    [MenuItem("Tools/Go To Hell/Balance Sheet", priority = 0)]
    public static void Open()
    {
        var w = GetWindow<BalanceSheetWindow>("Balance Sheet");
        w.minSize = new Vector2(900, 400);
        w.Reload();
    }

    private void OnEnable() => Reload();
    private void OnFocus() => Reload();
    private void OnProjectChange() => Reload();

    private void Reload()
    {
        upgradeRows = null;
        Repaint();
    }

    // ================================================================
    //  LAYOUT
    // ================================================================

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            tab = (Tab)GUILayout.Toolbar((int)tab, Enum.GetNames(typeof(Tab)), EditorStyles.toolbarButton, GUILayout.Width(360));
            GUILayout.Space(10);
            search = GUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(160));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reload", EditorStyles.toolbarButton)) Reload();
            if (GUILayout.Button("Save Assets", EditorStyles.toolbarButton)) AssetDatabase.SaveAssets();
        }

        switch (tab)
        {
            case Tab.Upgrades: DrawUpgrades(); break;
            case Tab.Enemies:  DrawEnemies();  break;
            case Tab.Pacts:    DrawPacts();    break;
            case Tab.Meta:     DrawMeta();     break;
        }
    }

    private bool Matches(params string[] fields)
    {
        if (string.IsNullOrEmpty(search)) return true;
        foreach (string f in fields)
            if (!string.IsNullOrEmpty(f) && f.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }

    private static void Header(params (string label, float width)[] cols)
    {
        using (new EditorGUILayout.HorizontalScope())
            foreach (var (label, width) in cols)
                GUILayout.Label(label, EditorStyles.boldLabel, width > 0 ? GUILayout.Width(width) : GUILayout.ExpandWidth(true));
        Rect line = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(line, new Color(0f, 0f, 0f, 0.4f));
    }

    /// <summary>
    /// One-line field for a property WITHOUT its [Header]/[Space] decorators
    /// (PropertyField draws those and breaks the row alignment).
    /// </summary>
    private static void Prop(SerializedObject so, string name, float width)
    {
        SerializedProperty p = so.FindProperty(name);
        GUILayoutOption w = GUILayout.Width(width);
        if (p == null) { GUILayout.Label("—", w); return; }

        EditorGUI.BeginChangeCheck();
        switch (p.propertyType)
        {
            case SerializedPropertyType.Integer:
                int i = EditorGUILayout.IntField(p.intValue, w);
                if (EditorGUI.EndChangeCheck()) p.intValue = i;
                return;
            case SerializedPropertyType.Float:
                float f = EditorGUILayout.FloatField(p.floatValue, w);
                if (EditorGUI.EndChangeCheck()) p.floatValue = f;
                return;
            case SerializedPropertyType.Boolean:
                bool b = EditorGUILayout.Toggle(p.boolValue, w);
                if (EditorGUI.EndChangeCheck()) p.boolValue = b;
                return;
            case SerializedPropertyType.String:
                string str = EditorGUILayout.TextField(p.stringValue, w);
                if (EditorGUI.EndChangeCheck()) p.stringValue = str;
                return;
            case SerializedPropertyType.Enum when name == "tags":
                var tags = (UpgradeTag)EditorGUILayout.EnumFlagsField((UpgradeTag)p.intValue, w);
                if (EditorGUI.EndChangeCheck()) p.intValue = (int)tags;
                return;
            case SerializedPropertyType.Enum:
                int e = EditorGUILayout.Popup(p.enumValueIndex, p.enumDisplayNames, w);
                if (EditorGUI.EndChangeCheck()) p.enumValueIndex = e;
                return;
            default:
                EditorGUI.EndChangeCheck();
                EditorGUILayout.PropertyField(p, GUIContent.none, w);
                return;
        }
    }

    /// <summary>String id field as a dropdown of known ids ("" = Any).</summary>
    private static void IdPopup(SerializedObject so, string name, string[] ids, float width)
    {
        SerializedProperty p = so.FindProperty(name);
        if (p == null) { GUILayout.Label("—", GUILayout.Width(width)); return; }
        string[] options = new[] { "Any" }.Concat(ids).ToArray();
        int cur = string.IsNullOrEmpty(p.stringValue) ? 0 : Array.IndexOf(options, p.stringValue);
        if (cur < 0) { options = options.Append(p.stringValue).ToArray(); cur = options.Length - 1; }
        int next = EditorGUILayout.Popup(cur, options, GUILayout.Width(width));
        if (next != cur) p.stringValue = next == 0 ? "" : options[next];
    }

    private static string[] Ids<T>(Func<T, string> id) where T : UnityEngine.Object =>
        FindAssets<T>().Select(id).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToArray();

    // ================================================================
    //  UPGRADES
    // ================================================================

    private static PlayerUpgradePool LoadPool() => Resources.Load<PlayerUpgradePool>("UpgradePool");

    private List<UpgradeRow> GetUpgradeRows()
    {
        if (upgradeRows != null) return upgradeRows;
        upgradeRows = new List<UpgradeRow>();

        PlayerUpgradePool pool = LoadPool();
        var inPool = new HashSet<GameObject>(pool != null ? pool.upgradePrefabs.Where(p => p != null) : Enumerable.Empty<GameObject>());

        IEnumerable<GameObject> prefabs = inPool;
        if (showAllProjectUpgrades)
            prefabs = AssetDatabase.FindAssets("t:Prefab")
                .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(go => go != null && go.GetComponent<PlayerUpgrade>() != null)
                .Union(inPool);

        foreach (GameObject prefab in prefabs.Distinct())
        {
            PlayerUpgrade u = prefab.GetComponent<PlayerUpgrade>();
            if (u == null) continue;
            upgradeRows.Add(new UpgradeRow { prefab = prefab, upgrade = u, so = new SerializedObject(u), inPool = inPool.Contains(prefab) });
        }
        upgradeRows.Sort((a, b) => a.upgrade.rarity != b.upgrade.rarity
            ? a.upgrade.rarity.CompareTo(b.upgrade.rarity)
            : string.CompareOrdinal(a.upgrade.displayName, b.upgrade.displayName));
        return upgradeRows;
    }

    private void DrawUpgrades()
    {
        PlayerUpgradePool pool = LoadPool();
        if (pool == null)
        {
            EditorGUILayout.HelpBox("No PlayerUpgradePool at Assets/Resources/UpgradePool.asset.", MessageType.Error);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            bool all = GUILayout.Toggle(showAllProjectUpgrades, "Include upgrade prefabs not in the pool", GUILayout.Width(260));
            if (all != showAllProjectUpgrades) { showAllProjectUpgrades = all; Reload(); }

            GUILayout.Label("Rarity", GUILayout.Width(40));
            string[] rarities = new[] { "All" }.Concat(Enum.GetNames(typeof(UpgradeRarity))).ToArray();
            rarityFilter = EditorGUILayout.Popup(rarityFilter + 1, rarities, GUILayout.Width(90)) - 1;
            GUILayout.Label("Tags", GUILayout.Width(32));
            tagFilter = (UpgradeTag)EditorGUILayout.EnumFlagsField(tagFilter, GUILayout.Width(140));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Export CSV", GUILayout.Width(90))) ExportUpgradesCsv();
            if (GUILayout.Button("Import CSV", GUILayout.Width(90))) ImportUpgradesCsv();
        }

        List<UpgradeRow> rows = GetUpgradeRows();
        DrawUpgradeSummary(rows);

        Header(("Pool", 34), ("Name", 150), ("Type", 160), ("Rarity", 85), ("Category", 85), ("Tags", 150),
               ("Weapon", 80), ("Pact", 90), ("Common", 55), ("Rare", 55), ("Epic", 55), ("Legendary", 70), ("", 0));
        weaponIds = Ids<WeaponDefinition>(d => d.weaponId);
        pactIds = Ids<PactDefinition>(d => d.pactId);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (UpgradeRow r in rows)
        {
            if (r.upgrade == null) continue;
            if (rarityFilter >= 0 && (int)r.upgrade.rarity != rarityFilter) continue;
            if (tagFilter != UpgradeTag.None && (r.upgrade.tags & tagFilter) == 0) continue;
            if (!Matches(r.upgrade.displayName, r.upgrade.UpgradeId, r.upgrade.tags.ToString())) continue;
            DrawUpgradeRow(r, pool);
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawUpgradeRow(UpgradeRow r, PlayerUpgradePool pool)
    {
        r.so.Update();
        string id = r.upgrade.UpgradeId;

        using (new EditorGUILayout.HorizontalScope())
        {
            bool pooled = EditorGUILayout.Toggle(r.inPool, GUILayout.Width(34));
            if (pooled != r.inPool) SetInPool(pool, r, pooled);

            Prop(r.so, "displayName", 150);
            GUILayout.Label(id, EditorStyles.miniLabel, GUILayout.Width(160));
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = UpgradeRarityRoller.GetRarityColor(r.upgrade.rarity);
            Prop(r.so, "rarity", 85);
            GUI.backgroundColor = old;
            Prop(r.so, "category", 85);
            if (r.upgrade.tags == UpgradeTag.None) GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            Prop(r.so, "tags", 150);
            GUI.backgroundColor = old;
            IdPopup(r.so, "requiredWeaponId", weaponIds, 80);
            IdPopup(r.so, "requiredPactId", pactIds, 90);
            Prop(r.so, "statCountCommon", 55);
            Prop(r.so, "statCountRare", 55);
            Prop(r.so, "statCountEpic", 55);
            Prop(r.so, "statCountLegendary", 70);

            bool open = expanded.Contains(id);
            if (GUILayout.Button(open ? "Tuning ▲" : "Tuning ▼", GUILayout.Width(75)))
            {
                if (open) expanded.Remove(id); else expanded.Add(id);
            }
            if (GUILayout.Button("Find", GUILayout.Width(45))) { EditorGUIUtility.PingObject(r.prefab); Selection.activeObject = r.prefab; }
        }

        if (expanded.Contains(id))
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.indentLevel++;
                bool any = false;
                foreach (SerializedProperty p in TuningProperties(r.so))
                {
                    EditorGUILayout.PropertyField(p, true);
                    any = true;
                }
                if (!any) EditorGUILayout.LabelField("No tuning fields.");
                EditorGUI.indentLevel--;
            }
        }

        if (r.so.ApplyModifiedProperties()) EditorUtility.SetDirty(r.upgrade);
    }

    /// <summary>Serialized fields declared by the upgrade subclass (not the PlayerUpgrade base).</summary>
    private static IEnumerable<SerializedProperty> TuningProperties(SerializedObject so)
    {
        var baseFields = new HashSet<string>(typeof(PlayerUpgrade)
            .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
            .Select(f => f.Name)) { "m_Script" };

        SerializedProperty it = so.GetIterator();
        bool enter = true;
        while (it.NextVisible(enter))
        {
            enter = false;
            if (baseFields.Contains(it.name)) continue;
            yield return it.Copy();
        }
    }

    private static void SetInPool(PlayerUpgradePool pool, UpgradeRow r, bool add)
    {
        Undo.RecordObject(pool, add ? "Add upgrade to pool" : "Remove upgrade from pool");
        if (add && !pool.upgradePrefabs.Contains(r.prefab)) pool.upgradePrefabs.Add(r.prefab);
        if (!add) pool.upgradePrefabs.Remove(r.prefab);
        EditorUtility.SetDirty(pool);
        r.inPool = add;
    }

    private void DrawUpgradeSummary(List<UpgradeRow> rows)
    {
        var pooled = rows.Where(r => r.inPool && r.upgrade != null).ToList();
        var sb = new StringBuilder();
        sb.Append($"{pooled.Count} in pool   ");
        foreach (UpgradeRarity rar in Enum.GetValues(typeof(UpgradeRarity)))
            sb.Append($"{rar}: {pooled.Count(r => r.upgrade.rarity == rar)}   ");

        var tagCounts = new List<string>();
        foreach (UpgradeTag t in Enum.GetValues(typeof(UpgradeTag)))
        {
            if (t == UpgradeTag.None) continue;
            int n = pooled.Count(r => (r.upgrade.tags & t) != 0);
            tagCounts.Add($"{t} {n}");
        }

        var warnings = new List<string>();
        int untagged = pooled.Count(r => r.upgrade.tags == UpgradeTag.None);
        if (untagged > 0) warnings.Add($"{untagged} untagged (red)");
        int noIcon = pooled.Count(r => r.upgrade.icon == null);
        if (noIcon > 0) warnings.Add($"{noIcon} without icon");
        foreach (var dup in pooled.GroupBy(r => r.upgrade.displayName).Where(g => g.Count() > 1))
            warnings.Add($"duplicate name '{dup.Key}'");
        int notPooled = rows.Count(r => !r.inPool);
        if (notPooled > 0) warnings.Add($"{notPooled} prefab(s) not in the pool (never offered)");

        EditorGUILayout.HelpBox(sb + "\nTags: " + string.Join(" · ", tagCounts) +
                                (warnings.Count > 0 ? "\n⚠ " + string.Join("   ⚠ ", warnings) : ""),
                                warnings.Count > 0 ? MessageType.Warning : MessageType.Info);
    }

    // ---- CSV (long format: Type, Field, Value) -----------------------------

    private void ExportUpgradesCsv()
    {
        string path = EditorUtility.SaveFilePanel("Export upgrade balance", "", "upgrade_balance.csv", "csv");
        if (string.IsNullOrEmpty(path)) return;

        var sb = new StringBuilder("Type,Field,Value\n");
        foreach (UpgradeRow r in GetUpgradeRows())
        {
            if (r.upgrade == null) continue;
            SerializedProperty it = r.so.GetIterator();
            bool enter = true;
            while (it.NextVisible(enter))
            {
                enter = false;
                if (it.name == "m_Script") continue;
                if (!TryGetCsvValue(it, out string value)) continue;
                sb.Append(Csv(r.upgrade.UpgradeId)).Append(',').Append(Csv(it.name)).Append(',').Append(Csv(value)).Append('\n');
            }
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        Debug.Log($"[BalanceSheet] Exported upgrade balance → {path}");
    }

    private void ImportUpgradesCsv()
    {
        string path = EditorUtility.OpenFilePanel("Import upgrade balance", "", "csv");
        if (string.IsNullOrEmpty(path)) return;

        var byType = GetUpgradeRows().Where(r => r.upgrade != null).ToDictionary(r => r.upgrade.UpgradeId);
        int changed = 0, skipped = 0;

        foreach (string line in File.ReadAllLines(path).Skip(1))
        {
            List<string> cells = ParseCsvLine(line);
            if (cells.Count < 3) continue;
            if (!byType.TryGetValue(cells[0], out UpgradeRow r)) { skipped++; continue; }

            SerializedProperty p = r.so.FindProperty(cells[1]);
            if (p == null || !TrySetCsvValue(p, cells[2])) { skipped++; continue; }
            if (r.so.ApplyModifiedProperties()) { EditorUtility.SetDirty(r.upgrade); changed++; }
        }

        AssetDatabase.SaveAssets();
        Reload();
        Debug.Log($"[BalanceSheet] Imported {changed} changed value(s), {skipped} skipped, from {path}");
    }

    private static bool TryGetCsvValue(SerializedProperty p, out string value)
    {
        switch (p.propertyType)
        {
            case SerializedPropertyType.Integer: value = p.intValue.ToString(); return true;
            case SerializedPropertyType.Float:   value = p.floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture); return true;
            case SerializedPropertyType.Boolean: value = p.boolValue ? "true" : "false"; return true;
            case SerializedPropertyType.String:  value = p.stringValue; return true;
            case SerializedPropertyType.Enum:    value = p.intValue.ToString(); return true;   // int: survives flags
            default: value = null; return false;
        }
    }

    private static bool TrySetCsvValue(SerializedProperty p, string s)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        switch (p.propertyType)
        {
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.Enum:
                if (!int.TryParse(s, out int i)) return false;
                if (p.intValue == i) return true;
                p.intValue = i; return true;
            case SerializedPropertyType.Float:
                if (!float.TryParse(s, System.Globalization.NumberStyles.Float, inv, out float f)) return false;
                p.floatValue = f; return true;
            case SerializedPropertyType.Boolean:
                if (!bool.TryParse(s, out bool b)) return false;
                p.boolValue = b; return true;
            case SerializedPropertyType.String:
                p.stringValue = s; return true;
            default: return false;
        }
    }

    private static string Csv(string s)
    {
        s ??= "";
        return s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var cells = new List<string>();
        var cur = new StringBuilder();
        bool quoted = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; }
                else if (c == '"') quoted = false;
                else cur.Append(c);
            }
            else if (c == '"') quoted = true;
            else if (c == ',') { cells.Add(cur.ToString()); cur.Clear(); }
            else cur.Append(c);
        }
        cells.Add(cur.ToString());
        return cells;
    }

    // ================================================================
    //  ENEMIES
    // ================================================================

    private void DrawEnemies()
    {
        List<EnemyDefinition> defs = FindAssets<EnemyDefinition>();

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Preview layer", GUILayout.Width(90));
            previewLayer = EditorGUILayout.IntSlider(previewLayer, 1, 9, GUILayout.Width(220));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Create definitions for enemy prefabs without one", GUILayout.Width(320)))
                GoToHellSetup.CreateEnemyDefinitions();
        }

        int missing = GoToHellSetup.EnemyPrefabsWithoutDefinition().Count;
        if (missing > 0)
            EditorGUILayout.HelpBox($"{missing} enemy prefab(s) still use their own Inspector numbers (no EnemyDefinition).", MessageType.Warning);
        if (defs.Count == 0)
        {
            EditorGUILayout.HelpBox("No EnemyDefinition assets yet. Use the button above.", MessageType.Info);
            return;
        }

        Header(("Id", 110), ("Name", 120), ("HP", 50), ("Contact", 55), ("Proj", 45), ("Speed×", 50),
               ("Souls min", 60), ("max", 45), ("Energy", 50), ("HP/layer", 60), ("Dmg/layer", 65), ("Souls/layer", 75),
               ($"@ Layer {previewLayer}", 0));

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (EnemyDefinition d in defs)
        {
            if (!Matches(d.enemyId, d.displayName, d.name)) continue;
            var so = new SerializedObject(d);
            so.Update();
            using (new EditorGUILayout.HorizontalScope())
            {
                Prop(so, "enemyId", 110);
                Prop(so, "displayName", 120);
                Prop(so, "maxHealth", 50);
                Prop(so, "contactDamage", 55);
                Prop(so, "projectileDamage", 45);
                Prop(so, "moveSpeedMultiplier", 50);
                Prop(so, "minSouls", 60);
                Prop(so, "maxSouls", 45);
                Prop(so, "energyOnDeath", 50);
                Prop(so, "healthPerLayer", 60);
                Prop(so, "damagePerLayer", 65);
                Prop(so, "soulsPerLayer", 75);

                int hp = Mathf.RoundToInt(d.maxHealth * d.HealthScale(previewLayer));
                int dmg = Mathf.RoundToInt(d.contactDamage * d.DamageScale(previewLayer));
                float souls = (d.minSouls + d.maxSouls) * 0.5f * d.SoulScale(previewLayer);
                GUILayout.Label($"HP {hp}  Dmg {dmg}  Souls ~{souls:0.#}", EditorStyles.miniBoldLabel);
                if (GUILayout.Button("Find", GUILayout.Width(45))) { EditorGUIUtility.PingObject(d); Selection.activeObject = d; }
            }
            if (so.ApplyModifiedProperties()) EditorUtility.SetDirty(d);
        }
        EditorGUILayout.EndScrollView();
    }

    // ================================================================
    //  PACTS
    // ================================================================

    private void DrawPacts()
    {
        List<PactDefinition> pacts = FindAssets<PactDefinition>();
        var unlockSources = new HashSet<PactDefinition>(FindAssets<AchievementDefinition>()
            .Where(a => a.pactToUnlock != null).Select(a => a.pactToUnlock));

        var dupIds = pacts.GroupBy(p => p.pactId).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (dupIds.Count > 0)
            EditorGUILayout.HelpBox("Duplicate pact ids: " + string.Join(", ", dupIds), MessageType.Error);
        if (pacts.Any(p => !AssetDatabase.GetAssetPath(p).Contains("/Resources/Pacts/")))
            EditorGUILayout.HelpBox("Some pacts are outside Assets/Resources/Pacts/ — the game won't find them.", MessageType.Warning);

        Header(("Id", 120), ("Name", 140), ("Skill component", 150), ("Order", 45), ("Default", 55), ("Unlock", 110), ("Description", 0));
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (PactDefinition p in pacts)
        {
            if (!Matches(p.pactId, p.displayName)) continue;
            var so = new SerializedObject(p);
            so.Update();
            using (new EditorGUILayout.HorizontalScope())
            {
                Prop(so, "pactId", 120);
                Prop(so, "displayName", 140);
                Prop(so, "skillComponentName", 150);
                Prop(so, "sortOrder", 45);
                Prop(so, "unlockedByDefault", 55);
                string unlock = p.unlockedByDefault ? "default" : unlockSources.Contains(p) ? "achievement" : "⚠ NONE";
                GUILayout.Label(unlock, GUILayout.Width(110));
                GUILayout.Label(p.description ?? "", EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button("Edit", GUILayout.Width(40))) Selection.activeObject = p;
            }
            if (so.ApplyModifiedProperties()) EditorUtility.SetDirty(p);
        }
        EditorGUILayout.EndScrollView();
    }

    // ================================================================
    //  META
    // ================================================================

    private void DrawMeta()
    {
        List<MetaUpgradeDefinition> metas = FindAssets<MetaUpgradeDefinition>()
            .OrderBy(m => m.sortOrder).ThenBy(m => m.displayName).ToList();
        MetaProgressionConfig cfg = Resources.Load<MetaProgressionConfig>("MetaProgressionConfig");

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            if (cfg == null)
                EditorGUILayout.HelpBox("No Assets/Resources/MetaProgressionConfig.asset — defaults are used. " +
                                        "Tools → Go To Hell → Data → Create Starter Progression Content makes one.", MessageType.Info);
            else if (GUILayout.Button("Edit MetaProgressionConfig", GUILayout.Width(200))) Selection.activeObject = cfg;

            GUILayout.Label("Economy preview — sample run", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                sampleKills = EditorGUILayout.IntField("Kills", sampleKills);
                sampleLayer = EditorGUILayout.IntField("Layer reached", sampleLayer);
                sampleLayersDone = EditorGUILayout.IntField("Layers done", sampleLayersDone);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                sampleLevel = EditorGUILayout.IntField("Player level", sampleLevel);
                sampleMinutes = EditorGUILayout.IntField("Minutes", sampleMinutes);
                sampleVictory = EditorGUILayout.Toggle("Victory", sampleVictory);
            }

            MetaProgressionConfig c = cfg != null ? cfg : CreateInstance<MetaProgressionConfig>();
            var run = new RunStats
            {
                enemiesKilled = sampleKills, deepestLayer = sampleLayer, layersCompleted = sampleLayersDone,
                finalLevel = sampleLevel, runDurationSeconds = sampleMinutes * 60f, victory = sampleVictory
            };
            float payout = c.BasePayout(run);
            int total = metas.Sum(m => m.TotalCost());
            EditorGUILayout.LabelField($"Payout ≈ {payout:0} {c.currencyName}   ·   Everything maxed costs {total}   ·   " +
                                       $"≈ {(payout > 0 ? Mathf.CeilToInt(total / payout) : 0)} such runs to buy it all",
                                       EditorStyles.boldLabel);
            if (cfg == null) DestroyImmediate(c);
        }

        Header(("Id", 120), ("Name", 130), ("Category", 80), ("Order", 45), ("Max", 40), ("Base cost", 65),
               ("Growth", 55), ("Rank costs", 170), ("Total", 50), ("Per rank", 0));
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (MetaUpgradeDefinition m in metas)
        {
            if (!Matches(m.metaId, m.displayName, m.category)) continue;
            var so = new SerializedObject(m);
            so.Update();
            using (new EditorGUILayout.HorizontalScope())
            {
                Prop(so, "metaId", 120);
                Prop(so, "displayName", 130);
                Prop(so, "category", 80);
                Prop(so, "sortOrder", 45);
                Prop(so, "maxRank", 40);
                Prop(so, "baseCost", 65);
                Prop(so, "costGrowth", 55);
                string costs = string.Join(" / ", Enumerable.Range(1, Mathf.Max(1, m.maxRank)).Select(m.CostForRank));
                GUILayout.Label(costs, EditorStyles.miniLabel, GUILayout.Width(170));
                GUILayout.Label(m.TotalCost().ToString(), GUILayout.Width(50));
                GUILayout.Label(m.Describe(1).Replace("\n", "  ·  "), EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button("Edit", GUILayout.Width(40))) Selection.activeObject = m;
            }
            if (so.ApplyModifiedProperties()) EditorUtility.SetDirty(m);
        }
        EditorGUILayout.EndScrollView();
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    private static List<T> FindAssets<T>() where T : UnityEngine.Object =>
        AssetDatabase.FindAssets("t:" + typeof(T).Name)
            .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(a => a != null)
            .ToList();
}
