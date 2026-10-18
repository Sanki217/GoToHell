using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Data, progression, UI-scaling and mobile tools (Tools → Go To Hell → …).
/// All of them are safe to re-run: they skip what already exists.
///
///   Data/Auto-Tag Upgrades                     fills empty UpgradeTag fields
///   Data/Create Enemy Definitions              one EnemyDefinition per enemy prefab
///   Data/Create Starter Progression Content    meta config + example meta upgrades
///   Characters/Build Main Menu Flow UI         Main Menu scene (slots, creation, pacts, meta shop)
///   Characters/Sync Pact Skills Into Class Prefabs
///   UI/Make Canvases Responsive                open scenes + all prefabs
///   UI/Convert EventSystems To Input System    open scenes + all prefabs
///   Mobile/Build Mobile Controls               gameplay scene
///   Mobile/Simulate Mobile                     run mobile code paths on desktop
/// </summary>
public static partial class GoToHellSetup
{
    // ================================================================
    //  DATA — UPGRADE TAGS
    // ================================================================

    private static readonly Dictionary<string, UpgradeTag> DefaultUpgradeTags = new Dictionary<string, UpgradeTag>
    {
        { "UpgradeArrowPierce",   UpgradeTag.Arrow | UpgradeTag.Projectile },
        { "UpgradeBloodArrow",    UpgradeTag.Arrow | UpgradeTag.Blood | UpgradeTag.Curse },
        { "UpgradeBurningWeapon", UpgradeTag.Fire },
        { "UpgradeDeadMansHand",  UpgradeTag.Arrow | UpgradeTag.Crit },
        { "UpgradeEnemyExplosion",UpgradeTag.Explosion | UpgradeTag.OnKill },
        { "UpgradeGravityArrow",  UpgradeTag.Arrow | UpgradeTag.Control },
        { "UpgradeImmunity",      UpgradeTag.Defense },
        { "UpgradeLifesteal",     UpgradeTag.Sustain | UpgradeTag.Blood },
        { "UpgradeMirrorArrow",   UpgradeTag.Arrow | UpgradeTag.Projectile },
        { "UpgradeNewSharpSet",   UpgradeTag.Arrow },
        { "UpgradePhantomStep",   UpgradeTag.Dash | UpgradeTag.Movement },
        { "UpgradePredator",      UpgradeTag.Dash | UpgradeTag.OnKill },
        { "UpgradeSoulArrow",     UpgradeTag.Arrow | UpgradeTag.Soul | UpgradeTag.OnKill | UpgradeTag.Projectile },
        { "UpgradeSoulBonus",     UpgradeTag.Soul | UpgradeTag.Economy },
    };

    [MenuItem("Tools/Go To Hell/Data/Auto-Tag Upgrades")]
    public static void AutoTagUpgrades()
    {
        int tagged = 0;
        var untagged = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            PlayerUpgrade u = prefab != null ? prefab.GetComponent<PlayerUpgrade>() : null;
            if (u == null || u.tags != UpgradeTag.None) continue;   // never overwrite hand-set tags

            if (DefaultUpgradeTags.TryGetValue(u.UpgradeId, out UpgradeTag tags))
            {
                Undo.RecordObject(u, "Auto-tag upgrade");
                u.tags = tags;
                EditorUtility.SetDirty(u);
                tagged++;
            }
            else untagged.Add(u.UpgradeId);
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[GoToHellSetup] Tagged {tagged} upgrade prefab(s)." +
                  (untagged.Count > 0 ? $" No default for: {string.Join(", ", untagged)} — tag them in the Balance Sheet." : ""));
    }

    // ================================================================
    //  DATA — ENEMY DEFINITIONS
    // ================================================================

    private const string EnemyDataFolder = "Assets/Data/Enemies";

    /// <summary>Enemy prefabs (combatants, not the training dummy) that have no EnemyDefinition yet.</summary>
    public static List<GameObject> EnemyPrefabsWithoutDefinition()
    {
        var list = new List<GameObject>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (prefab == null || !prefab.TryGetComponent(out Enemy e)) continue;
            if (e is TrainingDummy || !e.IsCombatant || e.definition != null) continue;
            list.Add(prefab);
        }
        return list;
    }

    [MenuItem("Tools/Go To Hell/Data/Create Enemy Definitions")]
    public static void CreateEnemyDefinitions()
    {
        List<GameObject> prefabs = EnemyPrefabsWithoutDefinition();
        if (prefabs.Count == 0)
        {
            Debug.Log("[GoToHellSetup] Every enemy prefab already has an EnemyDefinition.");
            return;
        }

        EnsureFolder(EnemyDataFolder);
        int created = 0;
        foreach (GameObject prefab in prefabs)
        {
            Enemy e = prefab.GetComponent<Enemy>();
            EnemyDefinition def = ScriptableObject.CreateInstance<EnemyDefinition>();
            def.enemyId = Slug(prefab.name);
            def.displayName = prefab.name;
            def.maxHealth = Mathf.Max(1, e.maxHealth);
            def.minSouls = e.minimumSouls;
            def.maxSouls = Mathf.Max(e.minimumSouls, e.maximumSouls);
            def.energyOnDeath = e.energyRestoredOnDeath;

            EnemyDamage contact = prefab.GetComponentInChildren<EnemyDamage>(true);
            def.contactDamage = contact != null ? contact.damage : 0;

            GameObject projectile = null;
            EnemyShooter shooter = prefab.GetComponentInChildren<EnemyShooter>(true);
            if (shooter != null) projectile = shooter.projectilePrefab;
            EnemyStalker stalker = prefab.GetComponentInChildren<EnemyStalker>(true);
            if (projectile == null && stalker != null) projectile = stalker.projectilePrefab;
            EnemyProjectile proj = projectile != null ? projectile.GetComponent<EnemyProjectile>() : null;
            def.projectileDamage = proj != null ? proj.damage : 0;

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{EnemyDataFolder}/{prefab.name}.asset");
            AssetDatabase.CreateAsset(def, assetPath);

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                Enemy root = scope.prefabContentsRoot.GetComponent<Enemy>();
                if (root != null) root.definition = def;
            }
            created++;
            Debug.Log($"[GoToHellSetup] {prefab.name} → {assetPath}");
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[GoToHellSetup] Created {created} EnemyDefinition(s) in {EnemyDataFolder}. Values copied from the prefabs — " +
                  "gameplay is unchanged on Layer 1; deeper layers now scale. Enemies placed directly in scenes " +
                  "(not prefabs) keep their own values until you assign a definition.");
    }

    private static string Slug(string s) =>
        Regex.Replace(s.Trim().ToLowerInvariant().Replace(' ', '_'), "[^a-z0-9_]", "");

    // ================================================================
    //  DATA — STARTER PROGRESSION CONTENT
    // ================================================================

    [MenuItem("Tools/Go To Hell/Data/Create Starter Progression Content")]
    public static void CreateStarterProgression()
    {
        EnsureFolder("Assets/Resources/Meta");
        int made = 0;

        if (Resources.Load<MetaProgressionConfig>("MetaProgressionConfig") == null)
        {
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<MetaProgressionConfig>(),
                                      "Assets/Resources/MetaProgressionConfig.asset");
            made++;
        }

        var existingMeta = new HashSet<string>(LoadAllAssets<MetaUpgradeDefinition>().Select(m => m.metaId));
        MetaUpgradeDefinition Meta(string id, string name, string desc, string cat, int order, int max, int cost, float growth,
                                   System.Action<RunModifierSet> setup)
        {
            if (existingMeta.Contains(id)) return LoadAllAssets<MetaUpgradeDefinition>().First(m => m.metaId == id);
            var m = ScriptableObject.CreateInstance<MetaUpgradeDefinition>();
            m.metaId = id; m.displayName = name; m.description = desc; m.category = cat; m.sortOrder = order;
            m.maxRank = max; m.baseCost = cost; m.costGrowth = growth;
            setup?.Invoke(m.perRank);
            AssetDatabase.CreateAsset(m, $"Assets/Resources/Meta/{name}.asset");
            made++;
            return m;
        }

        MetaUpgradeDefinition thickSkin =
        Meta("meta_thick_skin", "Thick Skin", "Scar tissue from a thousand falls.", "Body", 0, 5, 40, 1.5f,
             s => s.bonusMaxHP = 5);
        Meta("meta_sharpened", "Sharpened Sins", "Hate makes the arrow heavier.", "Body", 1, 5, 50, 1.5f,
             s => s.primaryStats.Add(new PrimaryStatAmount { stat = PrimaryStat.AttackDamage, amount = 0.5f }));
        Meta("meta_soul_hunger", "Soul Hunger", "Every soul is a little bigger.", "Soul", 10, 5, 60, 1.5f,
             s => s.soulGainPercent = 5);
        Meta("meta_quick_study", "Quick Study", "You learn faster in the dark.", "Soul", 11, 4, 60, 1.6f,
             s => s.xpGainPercent = 5);
        Meta("meta_haggler", "Haggler", "The merchant remembers your face.", "Soul", 12, 4, 50, 1.5f,
             s => s.shopPricePercent = -5);
        Meta("meta_nest_egg", "Nest Egg", "Souls hidden for the next descent.", "Soul", 13, 3, 70, 1.8f,
             s => s.startingSouls = 15);
        Meta("meta_devils_luck", "Devil's Luck", "Rarer gifts from below.", "Arsenal", 20, 3, 100, 2f,
             s => s.rarityLuck = 1);

        if (!existingMeta.Contains("meta_second_wind"))
        {
            MetaUpgradeDefinition wind = Meta("meta_second_wind", "Second Wind", "Hell spits you back out — once.", "Body", 5, 1, 400, 1f, null);
            wind.effects.Add(new ExtraLifeEffect());
            wind.requiredUpgrade = thickSkin;
            wind.requiredUpgradeRank = 3;
            EditorUtility.SetDirty(wind);
        }

        AssetDatabase.SaveAssets();
        Content.InvalidateAll();
        Debug.Log($"[GoToHellSetup] Starter progression: {made} asset(s) created (Resources/Meta, " +
                  "Resources/MetaProgressionConfig). Tune them in Tools → Go To Hell → Balance Sheet.");
    }

    // ================================================================
    //  CHARACTERS — CLASS KITS, PACT SKILLS, PREFAB SYNC
    // ================================================================

    private static readonly HashSet<string> RetiredPactIds = new HashSet<string>
    {
        "pact_wrath", "pact_frailty", "pact_haste", "pact_embers"
    };

    private static void ConfigureClassKit(ClassDefinition cls, WeaponDefinition weapon, string altComponent, string altName)
    {
        if (cls == null) return;
        cls.weapon = weapon;
        cls.altAttackComponentName = altComponent;
        cls.altAttackDisplayName = altName;
        EditorUtility.SetDirty(cls);
    }

    /// <summary>
    /// Pacts become RMB skills: removes the old modifier pacts, creates Dash +
    /// Shield, moves Dash-only upgrades from the Rogue class to the Dash pact,
    /// then syncs the skill components into every class prefab.
    /// </summary>
    [MenuItem("Tools/Go To Hell/Characters/Setup Pact Skills (Dash, Shield)")]
    public static void SetupPactSkills()
    {
        EnsureFolder("Assets/Resources/Pacts");

        foreach (PactDefinition p in LoadAllAssets<PactDefinition>())
        {
            bool retired = RetiredPactIds.Contains(p.pactId)
                           || (string.IsNullOrEmpty(p.skillComponentName) && string.IsNullOrEmpty(p.pactId));
            if (!retired) continue;
            string path = AssetDatabase.GetAssetPath(p);
            Debug.Log($"[GoToHellSetup] Removed old pact asset {path}");
            AssetDatabase.DeleteAsset(path);
        }

        EnsurePact("pact_dash", "Dash", "DashAbility", 0, true,
                   "Spend energy to dash toward the cursor, damaging enemies in your path.");
        EnsurePact("pact_shield", "Shield", "ShieldAbility", 1, false,   // unlocked by first death
                   "Hold RMB to raise a shield toward the cursor, blocking damage from that side. " +
                   "Drains energy while held. Size widens the arc.");

        // Upgrades that used to require a class skill now require the pact
        int moved = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            PlayerUpgrade u = prefab != null ? prefab.GetComponent<PlayerUpgrade>() : null;
            if (u == null) continue;
            string mapped = u.requiredPactId == "rogue" ? "pact_dash" : u.requiredPactId == "warrior" ? "pact_shield" : null;
            if (mapped == null) continue;
            u.requiredPactId = mapped;
            EditorUtility.SetDirty(u);
            moved++;
        }

        AssetDatabase.SaveAssets();
        Content.InvalidateAll();
        SyncPactSkillsIntoClassPrefabs();
        Debug.Log($"[GoToHellSetup] Pact skills ready (Dash, Shield). {moved} upgrade(s) moved from class to pact requirement.");
    }

    private static void EnsurePact(string id, string name, string component, int order, bool unlockedByDefault, string description)
    {
        PactDefinition p = LoadAllAssets<PactDefinition>().FirstOrDefault(x => x.pactId == id);
        if (p == null)
        {
            p = ScriptableObject.CreateInstance<PactDefinition>();
            p.pactId = id;
            AssetDatabase.CreateAsset(p, $"Assets/Resources/Pacts/{name}.asset");
        }
        p.displayName = name;
        p.skillComponentName = component;
        p.sortOrder = order;
        p.unlockedByDefault = unlockedByDefault;
        if (string.IsNullOrEmpty(p.description)) p.description = description;
        EditorUtility.SetDirty(p);
    }

    /// <summary>
    /// Every class prefab must carry every pact skill (disabled — RunConfigApplier
    /// enables the chosen one). Copies missing skill components from a prefab
    /// that has them, remapping references to children by hierarchy path.
    /// </summary>
    [MenuItem("Tools/Go To Hell/Characters/Sync Pact Skills Into Class Prefabs")]
    public static void SyncPactSkillsIntoClassPrefabs()
    {
        List<GameObject> prefabs = LoadAllAssets<ClassDefinition>()
            .Where(c => c.playerPrefab != null).Select(c => c.playerPrefab).Distinct().ToList();
        if (prefabs.Count == 0) { Debug.LogWarning("[GoToHellSetup] No class has a playerPrefab."); return; }

        var skillTypes = new List<System.Type>();
        foreach (PactDefinition p in LoadAllAssets<PactDefinition>())
        {
            if (string.IsNullOrEmpty(p.skillComponentName)) continue;
            System.Type t = TypeCache.GetTypesDerivedFrom<MonoBehaviour>().FirstOrDefault(x => x.Name == p.skillComponentName);
            if (t == null) Debug.LogWarning($"[GoToHellSetup] Pact '{p.pactId}': no component type '{p.skillComponentName}'.");
            else if (!skillTypes.Contains(t)) skillTypes.Add(t);
        }

        int added = 0;
        foreach (System.Type type in skillTypes)
        {
            GameObject source = prefabs.FirstOrDefault(pf => pf.GetComponent(type) != null);
            if (source == null)
            {
                Debug.LogWarning($"[GoToHellSetup] No class prefab has {type.Name} yet — add it to one prefab, then run this again.");
                continue;
            }

            foreach (GameObject target in prefabs)
            {
                if (target == source || target.GetComponent(type) != null) continue;

                using (var scope = new PrefabUtility.EditPrefabContentsScope(AssetDatabase.GetAssetPath(target)))
                {
                    GameObject root = scope.prefabContentsRoot;
                    Component src = source.GetComponent(type);
                    Component dst = root.AddComponent(type);
                    EditorUtility.CopySerialized(src, dst);
                    RemapReferences(dst, source.transform, root.transform);
                    if (dst is Behaviour b) b.enabled = false;
                }
                added++;
                Debug.Log($"[GoToHellSetup] Added {type.Name} (disabled) to {target.name}.");
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[GoToHellSetup] Pact skill sync done — {added} component(s) added.");
    }

    /// <summary>Points copied references at the same-path objects in the new prefab.</summary>
    private static void RemapReferences(Component comp, Transform sourceRoot, Transform targetRoot)
    {
        var so = new SerializedObject(comp);
        SerializedProperty it = so.GetIterator();
        while (it.Next(true))
        {
            if (it.propertyType != SerializedPropertyType.ObjectReference || it.objectReferenceValue == null) continue;
            if (it.name == "m_Script") continue;

            Object obj = it.objectReferenceValue;
            Transform t = obj is Component c ? c.transform : obj is GameObject g ? g.transform : null;
            if (t == null || t.root != sourceRoot) continue;   // assets / outside refs stay as they are

            string path = AnimationUtility.CalculateTransformPath(t, sourceRoot);
            Transform match = string.IsNullOrEmpty(path) ? targetRoot : targetRoot.Find(path);
            Object mapped = null;
            if (match != null) mapped = obj is GameObject ? (Object)match.gameObject : match.GetComponent(obj.GetType());
            if (mapped == null)
                Debug.LogWarning($"[GoToHellSetup] {comp.GetType().Name}.{it.propertyPath}: '{path}' not found in {targetRoot.name} — assign it by hand.");
            it.objectReferenceValue = mapped;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ================================================================
    //  CHARACTERS — MAIN MENU FLOW UI
    // ================================================================

    [MenuItem("Tools/Go To Hell/Characters/Build Main Menu Flow UI (Main Menu)")]
    public static void BuildMainMenuFlowUI()
    {
        MainMenu menu = Object.FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include);
        if (menu == null)
        {
            Debug.LogWarning("[GoToHellSetup] Open the Main Menu scene first (no MainMenu component found).");
            return;
        }
        foreach (string old in new[] { "ProgressionCanvas", "MenuFlowCanvas" })
            if (GameObject.Find(old) != null)
            {
                Debug.LogWarning($"[GoToHellSetup] Delete '{old}' first, then run this again.");
                return;
            }

        Canvas canvas = CreateCanvas("MenuFlowCanvas", 100);
        Transform root = canvas.transform;

        // Meta shop button + currency
        Button metaBtn = CreateButton(root, "MetaShopButton", "META SHOP", Vector2.zero, new Vector2(300, 80), 30);
        Anchor(metaBtn.gameObject, new Vector2(1, 0), new Vector2(-190, 130));
        TMP_Text currency = CreateText(root, "Currency", "0 Brimstone", 34, Vector2.zero, new Vector2(500, 60),
                                       TextAlignmentOptions.Right, FontStyles.Bold);
        Anchor(currency.gameObject, new Vector2(1, 1), new Vector2(-280, -60));
        currency.color = new Color(1f, 0.7f, 0.3f);

        // ---- Character select ----
        CharacterSelectUI select = canvas.gameObject.AddComponent<CharacterSelectUI>();
        select.panel = CreateModal(root, "CharacterSelectPanel", out Transform sw);
        CreateText(sw, "Header", "CHOOSE YOUR SINNER", 48, new Vector2(0, 400), new Vector2(1400, 70), TextAlignmentOptions.Center, FontStyles.Bold);
        RectTransform slots = CreateStack(sw, "Slots", new Vector2(-330, 0), new Vector2(760, 720));
        select.slotButtons = new Button[CharacterRoster.MaxCharacters];
        for (int i = 0; i < select.slotButtons.Length; i++)
            select.slotButtons[i] = ListRow(slots, $"Slot {i + 1}", "— empty slot —", 80f);
        select.detailText = CreateText(sw, "Detail", "", 28, new Vector2(430, 80), new Vector2(620, 560), TextAlignmentOptions.TopLeft);
        select.playButton = CreateButton(sw, "Play", "PLAY", new Vector2(430, -290), new Vector2(420, 90), 36);
        select.createButton = CreateButton(sw, "Create", "CREATE NEW", new Vector2(-560, -420), new Vector2(280, 64), 24);
        select.deleteButton = CreateButton(sw, "Delete", "DELETE", new Vector2(-250, -420), new Vector2(280, 64), 24);
        select.deleteButtonLabel = select.deleteButton.GetComponentInChildren<TMP_Text>();
        select.backButton = CreateButton(sw, "Back", "BACK", new Vector2(600, -420), new Vector2(220, 64), 24);

        // ---- Character create ----
        CharacterCreateUI create = canvas.gameObject.AddComponent<CharacterCreateUI>();
        create.panel = CreateModal(root, "CharacterCreatePanel", out Transform cw);
        CreateText(cw, "Header", "NEW CHARACTER", 48, new Vector2(0, 400), new Vector2(1400, 70), TextAlignmentOptions.Center, FontStyles.Bold);
        create.nameInput = CreateInputField(cw, "NameInput", CharacterRoster.DefaultName, new Vector2(-330, 300), new Vector2(760, 76));
        create.classContent = CreateScrollList(cw, new Vector2(-330, 90), new Vector2(760, 300));
        create.classRowTemplate = RowTemplate(create.classContent);
        create.classDetailText = CreateText(cw, "ClassDetail", "", 26, new Vector2(-330, -210), new Vector2(760, 260), TextAlignmentOptions.TopLeft);
        RectTransform stats = CreateStack(cw, "Stats", new Vector2(430, 110), new Vector2(620, 460));
        create.statRows = CharacterRoster.CreationStats.Select(st => StatRow(stats, st)).ToArray();
        create.pointsText = CreateText(cw, "Points", "", 30, new Vector2(350, -160), new Vector2(460, 50), TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        create.autoButton = CreateButton(cw, "Auto", "AUTO", new Vector2(665, -160), new Vector2(150, 50), 24);
        create.hardcoreToggle = CreateToggle(cw, "Hardcore", "Hardcore — death is permanent", new Vector2(430, -240), new Vector2(620, 50));
        create.createButton = CreateButton(cw, "Create", "CREATE", new Vector2(430, -340), new Vector2(360, 80), 32);
        create.cancelButton = CreateButton(cw, "Cancel", "CANCEL", new Vector2(600, -420), new Vector2(220, 64), 24);

        // ---- Pact select ----
        PactSelectUI pacts = canvas.gameObject.AddComponent<PactSelectUI>();
        pacts.panel = CreateModal(root, "PactSelectPanel", out Transform pw);
        pacts.headerText = CreateText(pw, "Header", "CHOOSE A PACT", 48, new Vector2(0, 400), new Vector2(1400, 70), TextAlignmentOptions.Center, FontStyles.Bold);
        pacts.content = CreateScrollList(pw, new Vector2(-330, 0), new Vector2(760, 720));
        pacts.rowTemplate = RowTemplate(pacts.content);
        pacts.detailText = CreateText(pw, "Detail", "", 28, new Vector2(430, 80), new Vector2(620, 560), TextAlignmentOptions.TopLeft);
        pacts.startButton = CreateButton(pw, "Descend", "DESCEND", new Vector2(430, -290), new Vector2(420, 90), 36);
        pacts.backButton = CreateButton(pw, "Back", "BACK", new Vector2(600, -420), new Vector2(220, 64), 24);

        select.createUI = create;
        select.pactSelect = pacts;

        // ---- Meta shop ----
        MetaShopUI shop = canvas.gameObject.AddComponent<MetaShopUI>();
        shop.panel = CreateModal(root, "MetaShopPanel", out Transform mw);
        CreateText(mw, "Header", "META SHOP", 48, new Vector2(-300, 400), new Vector2(800, 70), TextAlignmentOptions.Left, FontStyles.Bold);
        shop.currencyText = CreateText(mw, "Currency", "0", 36, new Vector2(400, 400), new Vector2(600, 70), TextAlignmentOptions.Right, FontStyles.Bold);
        shop.currencyText.color = new Color(1f, 0.7f, 0.3f);
        shop.content = CreateScrollList(mw, new Vector2(-330, -10), new Vector2(760, 700));
        shop.rowTemplate = RowTemplate(shop.content);
        shop.detailText = CreateText(mw, "Detail", "", 28, new Vector2(430, 60), new Vector2(620, 560), TextAlignmentOptions.TopLeft);
        shop.buyButton = CreateButton(mw, "Buy", "BUY", new Vector2(430, -300), new Vector2(420, 80), 30);
        shop.buyButtonLabel = shop.buyButton.GetComponentInChildren<TMP_Text>();
        shop.refundButton = CreateButton(mw, "Refund", "REFUND ALL", new Vector2(-560, -420), new Vector2(260, 60), 22);
        shop.closeButton = CreateButton(mw, "Close", "DONE", new Vector2(600, -420), new Vector2(220, 64), 24);

        select.panel.SetActive(false);
        create.panel.SetActive(false);
        pacts.panel.SetActive(false);
        shop.panel.SetActive(false);

        Undo.RecordObject(menu, "Wire main menu flow");
        menu.characterSelect = select;
        menu.metaShopButton = metaBtn;
        menu.metaShop = shop;
        menu.currencyText = currency;
        EditorUtility.SetDirty(menu);

        ApplyDefaultLook(canvas.gameObject);
        FinishScene(canvas.gameObject, "Main menu flow built: Play → Character Select → Create → Pact → run.");
    }

    /// <summary>A list row button that stretches to the list width (VerticalLayoutGroup parent).</summary>
    private static Button ListRow(Transform parent, string name, string label, float height)
    {
        Button b = CreateButton(parent, name, label, Vector2.zero, new Vector2(100, height), 24);
        b.gameObject.AddComponent<LayoutElement>().minHeight = height;
        TMP_Text t = b.GetComponentInChildren<TMP_Text>();
        t.alignment = TextAlignmentOptions.MidlineLeft;
        t.rectTransform.offsetMin = new Vector2(20, 0);
        return b;
    }

    /// <summary>Disabled "RowTemplate" inside a list — cloned at runtime once per entry, styled in the scene.</summary>
    private static GameObject RowTemplate(Transform list)
    {
        Button b = ListRow(list, "RowTemplate", "Row", 80f);
        b.gameObject.SetActive(false);
        return b.gameObject;
    }

    /// <summary>"Stat   1  [−] [+]" row for character creation.</summary>
    private static CharacterCreateUI.StatRow StatRow(Transform parent, PrimaryStat stat)
    {
        GameObject row = CreateUIObject(parent, CharacterCreateUI.StatName(stat));
        row.AddComponent<LayoutElement>().minHeight = 56;
        TMP_Text label = CreateText(row.transform, "Label", CharacterCreateUI.StatName(stat), 26, Vector2.zero,
                                    Vector2.zero, TextAlignmentOptions.MidlineLeft);
        StretchRect(label.gameObject);
        label.rectTransform.offsetMax = new Vector2(-140, 0);

        Button minus = CreateButton(row.transform, "Minus", "-", Vector2.zero, new Vector2(56, 48), 30);
        Anchor(minus.gameObject, new Vector2(1, 0.5f), new Vector2(-100, 0));
        Button plus = CreateButton(row.transform, "Plus", "+", Vector2.zero, new Vector2(56, 48), 30);
        Anchor(plus.gameObject, new Vector2(1, 0.5f), new Vector2(-35, 0));

        return new CharacterCreateUI.StatRow { stat = stat, label = label, minus = minus, plus = plus };
    }

    private static RectTransform CreateStack(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = CreateUIObject(parent, name);
        CenterRect(go, pos, size);
        VerticalLayoutGroup v = go.AddComponent<VerticalLayoutGroup>();
        v.spacing = 8;
        v.childControlHeight = true;
        v.childControlWidth = true;
        v.childForceExpandHeight = false;
        v.childForceExpandWidth = true;
        return go.GetComponent<RectTransform>();
    }

    private static Toggle CreateToggle(Transform parent, string name, string label, Vector2 pos, Vector2 size)
    {
        GameObject go = CreateUIObject(parent, name);
        CenterRect(go, pos, size);
        Toggle toggle = go.AddComponent<Toggle>();

        GameObject box = CreateUIObject(go.transform, "Box");
        RectTransform brt = box.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = new Vector2(0, 0.5f);
        brt.sizeDelta = new Vector2(40, 40);
        brt.anchoredPosition = new Vector2(30, 0);
        Image bg = box.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.19f, 0.24f, 1f);

        GameObject check = CreateUIObject(box.transform, "Check");
        StretchRect(check);
        check.GetComponent<RectTransform>().offsetMin = new Vector2(8, 8);
        check.GetComponent<RectTransform>().offsetMax = new Vector2(-8, -8);
        Image ck = check.AddComponent<Image>();
        ck.color = new Color(0.9f, 0.25f, 0.2f, 1f);

        toggle.targetGraphic = bg;
        toggle.graphic = ck;
        toggle.isOn = false;

        TextMeshProUGUI text = CreateText(go.transform, "Label", label, 26, Vector2.zero, size, TextAlignmentOptions.MidlineLeft);
        StretchRect(text.gameObject);
        text.rectTransform.offsetMin = new Vector2(64, 0);
        return toggle;
    }

    private static GameObject CreateModal(Transform parent, string name, out Transform window)
    {
        GameObject panel = CreatePanel(parent, name);
        Image dim = panel.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.85f);

        GameObject win = CreateUIObject(panel.transform, "Window");
        CenterRect(win, Vector2.zero, new Vector2(1600, 940));
        Image bg = win.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.07f, 0.09f, 0.98f);
        window = win.transform;
        return panel;
    }

    private static RectTransform CreateScrollList(Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject scrollGo = CreateUIObject(parent, "Scroll");
        CenterRect(scrollGo, pos, size);
        Image bg = scrollGo.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.04f);
        ScrollRect sr = scrollGo.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30f;

        GameObject viewport = CreateUIObject(scrollGo.transform, "Viewport");
        StretchRect(viewport);
        viewport.AddComponent<RectMask2D>();

        GameObject content = CreateUIObject(viewport.transform, "Content");
        RectTransform crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0, 1);
        crt.anchorMax = new Vector2(1, 1);
        crt.pivot = new Vector2(0.5f, 1);
        crt.sizeDelta = Vector2.zero;
        VerticalLayoutGroup v = content.AddComponent<VerticalLayoutGroup>();
        v.spacing = 8;
        v.padding = new RectOffset(10, 10, 10, 10);
        v.childControlHeight = true;
        v.childControlWidth = true;
        v.childForceExpandHeight = false;
        v.childForceExpandWidth = true;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.viewport = viewport.GetComponent<RectTransform>();
        sr.content = crt;
        return crt;
    }

    /// <summary>Anchors a centered UI object to a screen corner/edge, keeping its size.</summary>
    private static void Anchor(GameObject go, Vector2 anchor, Vector2 pos)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
    }

    // ================================================================
    //  UI — RESPONSIVE CANVASES / EVENT SYSTEMS
    // ================================================================

    [MenuItem("Tools/Go To Hell/UI/Make Canvases Responsive (open scenes + prefabs)")]
    public static void MakeCanvasesResponsive()
    {
        int changed = 0;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            int before = changed;
            foreach (GameObject rootGo in scene.GetRootGameObjects())
                foreach (CanvasScaler s in rootGo.GetComponentsInChildren<CanvasScaler>(true))
                    if (MakeResponsive(s, record: true)) changed++;
            if (changed != before) EditorSceneManager.MarkSceneDirty(scene);
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null || !asset.GetComponentsInChildren<CanvasScaler>(true).Any(s => NeedsResponsive(s))) continue;

            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
                foreach (CanvasScaler s in scope.prefabContentsRoot.GetComponentsInChildren<CanvasScaler>(true))
                    if (MakeResponsive(s, record: false)) changed++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[GoToHellSetup] {changed} canvas scaler(s) set to Scale With Screen Size 1920×1080. " +
                  "Open each other scene and run this again (only open scenes are processed). Save the scenes.");
    }

    private static bool IsScreenSpaceRoot(CanvasScaler s)
    {
        if (s == null || !s.TryGetComponent(out Canvas c) || c.renderMode == RenderMode.WorldSpace) return false;
        return c.GetComponentsInParent<Canvas>(true).Length == 1;   // root canvas
    }

    private static bool NeedsResponsive(CanvasScaler s) =>
        IsScreenSpaceRoot(s) &&
        (s.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize
         || s.referenceResolution != UIScaleManager.ReferenceResolution);

    private static bool MakeResponsive(CanvasScaler s, bool record)
    {
        if (!NeedsResponsive(s)) return false;
        if (record) Undo.RecordObject(s, "Make canvas responsive");
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = UIScaleManager.ReferenceResolution;
        s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        s.matchWidthOrHeight = 0.5f;   // editor preview; UIScaleManager picks per aspect at runtime
        EditorUtility.SetDirty(s);
        return true;
    }

    [MenuItem("Tools/Go To Hell/UI/Convert EventSystems To Input System (open scenes + prefabs)")]
    public static void ConvertEventSystems()
    {
#if ENABLE_INPUT_SYSTEM
        int changed = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            int before = changed;
            foreach (GameObject rootGo in scene.GetRootGameObjects())
                foreach (UnityEngine.EventSystems.EventSystem es in rootGo.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true))
                    if (InputModuleBridge.Fix(es.gameObject)) changed++;
            if (changed != before) EditorSceneManager.MarkSceneDirty(scene);
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null || asset.GetComponentInChildren<UnityEngine.EventSystems.StandaloneInputModule>(true) == null) continue;
            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
                foreach (var es in scope.prefabContentsRoot.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true))
                    if (InputModuleBridge.Fix(es.gameObject)) changed++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[GoToHellSetup] Converted {changed} EventSystem(s) to InputSystemUIInputModule. Save the scenes; repeat per scene.");
#else
        Debug.LogWarning("[GoToHellSetup] Active Input Handling doesn't include the Input System — nothing to convert.");
#endif
    }

    // ================================================================
    //  MOBILE
    // ================================================================

    private const string SimulateMobileMenu = "Tools/Go To Hell/Mobile/Simulate Mobile";

    [MenuItem(SimulateMobileMenu)]
    public static void ToggleSimulateMobile()
    {
        Platform.SimulateMobile = !Platform.SimulateMobile;
        Debug.Log($"[GoToHellSetup] Simulate Mobile: {(Platform.SimulateMobile ? "ON" : "OFF")} " +
                  "(touch controls, safe areas and touch UI scale active in Play Mode).");
    }

    [MenuItem(SimulateMobileMenu, true)]
    private static bool ToggleSimulateMobileValidate()
    {
        Menu.SetChecked(SimulateMobileMenu, Platform.SimulateMobile);
        return true;
    }

    [MenuItem("Tools/Go To Hell/Mobile/Build Mobile Controls (gameplay scene)")]
    public static void BuildMobileControls()
    {
#if ENABLE_INPUT_SYSTEM
        if (Object.FindFirstObjectByType<MobileControls>(FindObjectsInactive.Include) != null)
        {
            Debug.LogWarning("[GoToHellSetup] This scene already has MobileControls.");
            return;
        }

        Canvas canvas = CreateCanvas("MobileControlsCanvas", 50);
        MobileControls mc = canvas.gameObject.AddComponent<MobileControls>();

        GameObject safe = CreatePanel(canvas.transform, "SafeArea");
        safe.AddComponent<SafeArea>();
        GameObject controls = CreatePanel(safe.transform, "Controls");
        mc.controlsRoot = controls;

        Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        MobileStick(controls.transform, "MoveStick", new Vector2(0, 0), new Vector2(230, 230), "<Gamepad>/leftStick", knob);
        MobileStick(controls.transform, "AimStick", new Vector2(1, 0), new Vector2(-230, 230), "<Gamepad>/rightStick", knob);

        MobileButton(controls.transform, "Jump",     "JUMP",   new Vector2(1, 0), new Vector2(-500, 110), "<Gamepad>/buttonSouth", knob);
        MobileButton(controls.transform, "Attack",   "FIRE",   new Vector2(1, 0), new Vector2(-500, 290), "<Gamepad>/rightTrigger", knob);
        MobileButton(controls.transform, "Skill",    "SKILL",  new Vector2(1, 0), new Vector2(-230, 480), "<Gamepad>/rightShoulder", knob);
        MobileButton(controls.transform, "Sword",    "SWORD",  new Vector2(1, 0), new Vector2(-410, 470), "<Gamepad>/leftTrigger", knob);
        MobileButton(controls.transform, "Interact", "USE",    new Vector2(0, 0), new Vector2(480, 130), "<Gamepad>/buttonNorth", knob);
        MobileButton(controls.transform, "LevelUp",  "LEVEL",  new Vector2(0.5f, 1), new Vector2(0, -90), "<Gamepad>/buttonWest", knob);
        MobileButton(controls.transform, "Pause",    "II",     new Vector2(1, 1), new Vector2(-90, -90), "<Gamepad>/start", knob);

        mc.showOnDesktop = false;
        FinishScene(canvas.gameObject, "Mobile controls built (hidden on desktop — tick 'Show On Desktop' or use Simulate Mobile to see them).");
#else
        Debug.LogWarning("[GoToHellSetup] Mobile controls need the Input System (Active Input Handling = Input System or Both).");
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static void MobileStick(Transform parent, string name, Vector2 anchor, Vector2 pos, string path, Sprite sprite)
    {
        GameObject bg = CreateUIObject(parent, name);
        CenterRect(bg, Vector2.zero, new Vector2(280, 280));
        Anchor(bg, anchor, pos);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.sprite = sprite;
        bgImg.color = new Color(1f, 1f, 1f, 0.15f);
        bgImg.raycastTarget = false;

        GameObject handle = CreateUIObject(bg.transform, "Handle");
        CenterRect(handle, Vector2.zero, new Vector2(130, 130));
        Image h = handle.AddComponent<Image>();
        h.sprite = sprite;
        h.color = new Color(1f, 1f, 1f, 0.45f);
        var stick = handle.AddComponent<UnityEngine.InputSystem.OnScreen.OnScreenStick>();
        stick.controlPath = path;
        stick.movementRange = 110f;
    }

    private static void MobileButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, string path, Sprite sprite)
    {
        GameObject go = CreateUIObject(parent, name);
        CenterRect(go, Vector2.zero, new Vector2(150, 150));
        Anchor(go, anchor, pos);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = new Color(1f, 1f, 1f, 0.3f);
        var button = go.AddComponent<UnityEngine.InputSystem.OnScreen.OnScreenButton>();
        button.controlPath = path;

        TextMeshProUGUI text = CreateText(go.transform, "Label", label, 30, Vector2.zero, new Vector2(150, 150),
                                          TextAlignmentOptions.Center, FontStyles.Bold);
        StretchRect(text.gameObject);
    }
#endif
}
