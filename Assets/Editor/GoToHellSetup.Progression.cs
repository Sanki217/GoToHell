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
///   Data/Create Starter Progression Content    config + example meta upgrades + pacts
///   Progression/Build Pact + Meta Shop UI      Main Menu scene
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
        EnsureFolder("Assets/Resources/Pacts");
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

        var existingPacts = new HashSet<string>(LoadAllAssets<PactDefinition>().Select(p => p.pactId));
        PactDefinition Pact(string id, string name, string desc, int heat, System.Action<RunModifierSet> setup)
        {
            if (existingPacts.Contains(id)) return null;
            var p = ScriptableObject.CreateInstance<PactDefinition>();
            p.pactId = id; p.displayName = name; p.description = desc; p.heat = heat;
            p.unlockedByDefault = true;   // starter pacts: switch to achievement unlocks when ready
            setup?.Invoke(p.modifiers);
            AssetDatabase.CreateAsset(p, $"Assets/Resources/Pacts/{name}.asset");
            made++;
            return p;
        }

        Pact("pact_wrath", "Pact of Wrath", "The damned grow strong. So do their souls.", 2,
             s => { s.enemyHealthPercent = 40; s.soulGainPercent = 25; });
        Pact("pact_frailty", "Pact of Frailty", "Bleed more, learn more.", 2,
             s => { s.damageTakenPercent = 50; s.xpGainPercent = 25; });
        Pact("pact_haste", "Pact of Haste", "Everything below moves faster.", 1,
             s => { s.enemySpeedPercent = 25; s.metaCurrencyPercent = 15; });
        PactDefinition embers = Pact("pact_embers", "Pact of Embers", "Burn bright. Fire upgrades find you.", 0,
             s => { s.favouredTags = UpgradeTag.Fire; s.favouredTagWeightPercent = 150; s.damageTakenPercent = 10; });
        if (embers != null)
        {
            PlayerUpgradePool pool = Resources.Load<PlayerUpgradePool>("UpgradePool");
            GameObject burning = pool != null
                ? pool.upgradePrefabs.FirstOrDefault(p => p != null && p.GetComponent<UpgradeBurningWeapon>() != null)
                : null;
            if (burning != null) embers.effects.Add(new GrantUpgradeEffect { upgradePrefab = burning });
            EditorUtility.SetDirty(embers);
        }

        AssetDatabase.SaveAssets();
        Content.InvalidateAll();
        Debug.Log($"[GoToHellSetup] Starter progression: {made} asset(s) created (Resources/Meta, Resources/Pacts, " +
                  "Resources/MetaProgressionConfig). Tune them in Tools → Go To Hell → Balance Sheet.");
    }

    // ================================================================
    //  PROGRESSION — PACT + META SHOP UI (Main Menu)
    // ================================================================

    [MenuItem("Tools/Go To Hell/Progression/Build Pact + Meta Shop UI (Main Menu)")]
    public static void BuildProgressionUI()
    {
        MainMenu menu = Object.FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include);
        if (menu == null)
        {
            Debug.LogWarning("[GoToHellSetup] Open the Main Menu scene first (no MainMenu component found).");
            return;
        }
        if (GameObject.Find("ProgressionCanvas") != null)
        {
            Debug.LogWarning("[GoToHellSetup] 'ProgressionCanvas' already exists — delete it first to rebuild.");
            return;
        }

        Canvas canvas = CreateCanvas("ProgressionCanvas", 100);
        Transform root = canvas.transform;

        // Menu buttons + currency (bottom-right / top-right)
        Button pactsBtn = CreateButton(root, "PactsButton", "PACTS", Vector2.zero, new Vector2(300, 80), 30);
        Anchor(pactsBtn.gameObject, new Vector2(1, 0), new Vector2(-190, 230));
        Button metaBtn = CreateButton(root, "MetaShopButton", "META SHOP", Vector2.zero, new Vector2(300, 80), 30);
        Anchor(metaBtn.gameObject, new Vector2(1, 0), new Vector2(-190, 130));
        TMP_Text currency = CreateText(root, "Currency", "0 Brimstone", 34, Vector2.zero, new Vector2(500, 60),
                                       TextAlignmentOptions.Right, FontStyles.Bold);
        Anchor(currency.gameObject, new Vector2(1, 1), new Vector2(-280, -60));
        currency.color = new Color(1f, 0.7f, 0.3f);

        // Pact panel
        PactSelectUI pacts = canvas.gameObject.AddComponent<PactSelectUI>();
        GameObject pactPanel = CreateModal(root, "PactPanel", out Transform pactWin);
        pacts.panel = pactPanel;
        pacts.headerText = CreateText(pactWin, "Header", "PACTS", 48, new Vector2(0, 400), new Vector2(1400, 70), TextAlignmentOptions.Center, FontStyles.Bold);
        pacts.content = CreateScrollList(pactWin, new Vector2(-330, -10), new Vector2(760, 700));
        pacts.detailText = CreateText(pactWin, "Detail", "Pick a pact to see what it does.", 28, new Vector2(430, -10), new Vector2(620, 700), TextAlignmentOptions.TopLeft);
        pacts.closeButton = CreateButton(pactWin, "Close", "DONE", new Vector2(0, -420), new Vector2(260, 70), 28);

        // Meta shop panel
        MetaShopUI shop = canvas.gameObject.AddComponent<MetaShopUI>();
        GameObject shopPanel = CreateModal(root, "MetaShopPanel", out Transform shopWin);
        shop.panel = shopPanel;
        CreateText(shopWin, "Header", "META SHOP", 48, new Vector2(-300, 400), new Vector2(800, 70), TextAlignmentOptions.Left, FontStyles.Bold);
        shop.currencyText = CreateText(shopWin, "Currency", "0", 36, new Vector2(400, 400), new Vector2(600, 70), TextAlignmentOptions.Right, FontStyles.Bold);
        shop.currencyText.color = new Color(1f, 0.7f, 0.3f);
        shop.content = CreateScrollList(shopWin, new Vector2(-330, -10), new Vector2(760, 700));
        shop.detailText = CreateText(shopWin, "Detail", "", 28, new Vector2(430, 60), new Vector2(620, 560), TextAlignmentOptions.TopLeft);
        shop.buyButton = CreateButton(shopWin, "Buy", "BUY", new Vector2(430, -300), new Vector2(420, 80), 30);
        shop.buyButtonLabel = shop.buyButton.GetComponentInChildren<TMP_Text>();
        shop.refundButton = CreateButton(shopWin, "Refund", "REFUND ALL", new Vector2(-560, -420), new Vector2(260, 60), 22);
        shop.closeButton = CreateButton(shopWin, "Close", "DONE", new Vector2(0, -420), new Vector2(260, 70), 28);

        pactPanel.SetActive(false);
        shopPanel.SetActive(false);

        Undo.RecordObject(menu, "Wire progression UI");
        menu.pactsButton = pactsBtn;
        menu.pactSelect = pacts;
        menu.metaShopButton = metaBtn;
        menu.metaShop = shop;
        menu.currencyText = currency;
        EditorUtility.SetDirty(menu);

        FinishScene(canvas.gameObject, "Pact + Meta Shop UI built and wired to MainMenu.");
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
