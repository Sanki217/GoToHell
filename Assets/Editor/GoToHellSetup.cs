using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One-shot scene/asset builders for the character-creation flow.
/// Run from the menu, in this order:
///
///   Tools → Go To Hell → 1. Create Default Assets        (any scene)
///   Tools → Go To Hell → 2. Build Character Creator UI   (open "Character Creator" scene)
///   Tools → Go To Hell → 3. Build Run Summary UI         (open "Level 1" scene)
///   Tools → Go To Hell → 4. Build Collection UI          (open "Collection" scene)
///
/// Each builder creates a fresh, fully wired canvas (it never edits your existing
/// objects) — restyle/move everything freely afterwards; only the component
/// references matter. Safe to re-run: skips anything that already exists.
/// </summary>
public static class GoToHellSetup
{
    // ================================================================
    //  0. DELETE SAVE
    // ================================================================

    [MenuItem("Tools/Go To Hell/0. Delete Save File")]
    public static void DeleteSaveFile()
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, "save.json");
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
            Debug.Log("[GoToHellSetup] Deleted save: " + path);
        }
        else
        {
            Debug.Log("[GoToHellSetup] No save file at: " + path);
        }
    }

    // ================================================================
    //  1. DEFAULT ASSETS
    // ================================================================

    [MenuItem("Tools/Go To Hell/1. Create Default Assets")]
    public static void CreateDefaultAssets()
    {
        EnsureFolder("Assets/Resources/Classes");
        EnsureFolder("Assets/Resources/Weapons");

        // Rogue
        const string roguePath = "Assets/Resources/Classes/Rogue.asset";
        if (AssetDatabase.LoadAssetAtPath<ClassDefinition>(roguePath) == null)
        {
            ClassDefinition rogue = ScriptableObject.CreateInstance<ClassDefinition>();
            rogue.classId            = "rogue";
            rogue.displayName        = "Rogue";
            rogue.description        = "A nimble sinner who trusts speed over armour.";
            rogue.unlockedByDefault  = true;
            rogue.skillComponentName = "DashAbility";
            rogue.skillDisplayName   = "Dash";
            rogue.skillDescription   = "Spend energy to dash toward the cursor, damaging enemies in your path.";
            rogue.agility = 3; rogue.attackDamage = 2; rogue.luck = 1; rogue.psyche = 2;
            rogue.health = 1;  rogue.size = 0;         rogue.cooldown = 1;
            AssetDatabase.CreateAsset(rogue, roguePath);
            Debug.Log("[GoToHellSetup] Created Rogue class (assign its sprite in the Inspector).");
        }

        // Warrior
        const string warriorPath = "Assets/Resources/Classes/Warrior.asset";
        if (AssetDatabase.LoadAssetAtPath<ClassDefinition>(warriorPath) == null)
        {
            ClassDefinition warrior = ScriptableObject.CreateInstance<ClassDefinition>();
            warrior.classId            = "warrior";
            warrior.displayName        = "Warrior";
            warrior.description        = "A bulwark of muscle and spite. Slower, but very hard to put down.";
            warrior.unlockedByDefault  = false;   // unlocked by the "die once" achievement
            warrior.skillComponentName = "ShieldAbility";
            warrior.skillDisplayName   = "Shield";
            warrior.skillDescription   = "Hold RMB to raise a shield toward the cursor, blocking damage from that direction. Drains energy per second. Size widens the arc.";
            warrior.agility = 1; warrior.attackDamage = 2; warrior.luck = 0; warrior.psyche = 1;
            warrior.health = 4;  warrior.size = 1;         warrior.cooldown = 1;
            AssetDatabase.CreateAsset(warrior, warriorPath);
            Debug.Log("[GoToHellSetup] Created Warrior class (assign its sprite in the Inspector).");
        }

        // Bow (create, or upgrade an existing asset with the new component list)
        const string bowPath = "Assets/Resources/Weapons/Bow.asset";
        string bowDescription = "Charged shots with a finite quiver — arrows stick to the world and can be reclaimed. The quiver slowly regenerates over time.";
        WeaponDefinition bowDef = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(bowPath);
        if (bowDef == null)
        {
            bowDef = ScriptableObject.CreateInstance<WeaponDefinition>();
            bowDef.weaponId          = "bow";
            bowDef.displayName       = "Bow";
            bowDef.description       = bowDescription;
            bowDef.unlockedByDefault = true;
            bowDef.weaponComponentNames = new[] { "PlayerShooting", "ArrowRegenerator" };
            AssetDatabase.CreateAsset(bowDef, bowPath);
            Debug.Log("[GoToHellSetup] Created Bow weapon (assign its sprite in the Inspector).");
        }
        else if (bowDef.weaponComponentNames == null || bowDef.weaponComponentNames.Length == 0)
        {
            bowDef.weaponComponentNames = new[] { "PlayerShooting", "ArrowRegenerator" };
            bowDef.description = bowDescription;
            EditorUtility.SetDirty(bowDef);
            Debug.Log("[GoToHellSetup] Updated Bow with weapon components (PlayerShooting, ArrowRegenerator).");
        }

        // Sword
        const string swordPath = "Assets/Resources/Weapons/Sword.asset";
        if (AssetDatabase.LoadAssetAtPath<WeaponDefinition>(swordPath) == null)
        {
            WeaponDefinition sword = ScriptableObject.CreateInstance<WeaponDefinition>();
            sword.weaponId          = "sword";
            sword.displayName       = "Sword";
            sword.description       = "A close-quarters blade. Slash toward the cursor — reach scales with Size, speed with Cooldown.";
            sword.unlockedByDefault = false;   // unlocked by the "finish Level 1" achievement
            sword.weaponComponentNames = new[] { "PlayerSlash" };
            AssetDatabase.CreateAsset(sword, swordPath);
            Debug.Log("[GoToHellSetup] Created Sword weapon (assign its sprite in the Inspector).");
        }

        // DEMO LOCK STATE — only Rogue + Bow start unlocked.
        // Warrior/Sword unlock via achievements; pacts arrive after the first boss.
        ClassDefinition warriorDef = AssetDatabase.LoadAssetAtPath<ClassDefinition>(warriorPath);
        if (warriorDef != null && warriorDef.unlockedByDefault)
        {
            warriorDef.unlockedByDefault = false;
            EditorUtility.SetDirty(warriorDef);
            Debug.Log("[GoToHellSetup] Warrior locked (achievement: die once).");
        }
        WeaponDefinition swordDef = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(swordPath);
        if (swordDef != null && swordDef.unlockedByDefault)
        {
            swordDef.unlockedByDefault = false;
            EditorUtility.SetDirty(swordDef);
            Debug.Log("[GoToHellSetup] Sword locked (achievement: finish Level 1).");
        }
        foreach (PactDefinition p in Resources.LoadAll<PactDefinition>("Pacts"))
        {
            if (p != null && p.unlockedByDefault)
            {
                p.unlockedByDefault = false;
                EditorUtility.SetDirty(p);
                Debug.Log($"[GoToHellSetup] Pact '{p.name}' locked (not in demo).");
            }
        }

        // Unlock achievements
        EnsureFolder("Assets/Resources/Achievements");
        EnsureUnlockAchievement("ach_first_death", "Death Is Just The Beginning",
            "Die for the first time.", "player_died", warriorDef, null);
        EnsureUnlockAchievement("ach_limbo_cleared", "Limbo Cleared",
            "Finish Level 1 for the first time.", "level_1_complete", null, swordDef);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[GoToHellSetup] Default assets done.");
    }

    // ================================================================
    //  1b. UPGRADE AVAILABILITY TAGS
    // ================================================================

    [MenuItem("Tools/Go To Hell/1b. Tag Upgrade Availability")]
    public static void TagUpgradeAvailability()
    {
        PlayerUpgradePool pool = Resources.Load<PlayerUpgradePool>("UpgradePool");
        if (pool == null)
        {
            Debug.LogError("[GoToHellSetup] No PlayerUpgradePool at Resources/UpgradePool.");
            return;
        }

        // Bow-only upgrades (arrow mechanics)
        var bowUpgrades = new System.Collections.Generic.HashSet<string>
        {
            "UpgradeArrowPierce", "UpgradeGravityArrow", "UpgradeMirrorArrow",
            "UpgradeSoulArrow", "UpgradeNewSharpSet", "UpgradeDeadMansHand",
            "UpgradeBloodArrow"
        };
        // Rogue-only upgrades (dash mechanics)
        var rogueUpgrades = new System.Collections.Generic.HashSet<string>
        {
            "UpgradePredator", "UpgradePhantomStep"
        };

        int tagged = 0;
        foreach (GameObject prefab in pool.upgradePrefabs)
        {
            if (prefab == null) continue;
            PlayerUpgrade u = prefab.GetComponent<PlayerUpgrade>();
            if (u == null) continue;

            string typeName = u.GetType().Name;
            string weapon = bowUpgrades.Contains(typeName) ? "bow" : "";
            string cls = rogueUpgrades.Contains(typeName) ? "rogue" : "";

            bool dirty = false;
            if (u.requiredWeaponId != weapon) { u.requiredWeaponId = weapon; dirty = true; }
            if (u.requiredClassId != cls) { u.requiredClassId = cls; dirty = true; }

            // Burning Arrow → Burning Weapon display rebrand (same prefab/GUID)
            if (typeName == "UpgradeBurningWeapon" && u.displayName != "Burning Weapon")
            {
                u.displayName = "Burning Weapon";
                u.description = "Your weapon hits set enemies on fire. Works with arrows, slashes, and dashes.";
                dirty = true;
            }

            if (dirty)
            {
                EditorUtility.SetDirty(u);
                tagged++;
                Debug.Log($"[GoToHellSetup] Tagged {typeName}: weapon='{weapon}' class='{cls}'.");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[GoToHellSetup] Upgrade tagging done ({tagged} prefabs updated).");
    }

    private static void EnsureUnlockAchievement(string id, string displayName, string description,
        string eventId, ClassDefinition classUnlock, WeaponDefinition weaponUnlock)
    {
        string path = $"Assets/Resources/Achievements/{id}.asset";
        AchievementDefinition existing = AssetDatabase.LoadAssetAtPath<AchievementDefinition>(path);
        if (existing != null)
        {
            bool dirty = false;
            if (existing.classToUnlock == null && classUnlock != null) { existing.classToUnlock = classUnlock; dirty = true; }
            if (existing.weaponToUnlock == null && weaponUnlock != null) { existing.weaponToUnlock = weaponUnlock; dirty = true; }
            if (dirty) EditorUtility.SetDirty(existing);
            return;
        }

        AchievementDefinition a = ScriptableObject.CreateInstance<AchievementDefinition>();
        a.id = id;
        a.displayName = displayName;
        a.description = description;
        a.type = AchievementType.Event;
        a.eventId = eventId;
        a.classToUnlock = classUnlock;
        a.weaponToUnlock = weaponUnlock;
        AssetDatabase.CreateAsset(a, path);
        Debug.Log($"[GoToHellSetup] Created achievement '{id}' ({eventId}).");
    }

    // ================================================================
    //  2. CHARACTER CREATOR UI
    // ================================================================

    [MenuItem("Tools/Go To Hell/2. Build Character Creator UI")]
    public static void BuildCharacterCreatorUI()
    {
        if (GameObject.Find("CharacterCreatorUI") != null)
        {
            Debug.LogWarning("[GoToHellSetup] 'CharacterCreatorUI' already exists in this scene — delete it first to rebuild.");
            return;
        }

        Canvas canvas = CreateCanvas("CharacterCreatorUI", 10);
        Transform root = canvas.transform;

        // ── Panels ──────────────────────────────────────────────────
        GameObject classPanel  = CreatePanel(root, "ClassPanel");
        GameObject weaponPanel = CreatePanel(root, "WeaponPanel");
        GameObject pactPanel   = CreatePanel(root, "PactPanel");
        GameObject namePanel   = CreatePanel(root, "NamePanel");

        // ── Class stage ─────────────────────────────────────────────
        CreateText(classPanel.transform, "Title", "CHOOSE YOUR CLASS", 56, new Vector2(0, 460), new Vector2(1200, 80), TextAlignmentOptions.Center, FontStyles.Bold);
        CreatorCarousel classCarousel = BuildCarousel(classPanel);
        Button classNext = CreateButton(classPanel.transform, "NextButton", "NEXT", new Vector2(760, -480), new Vector2(220, 70));

        // ── Weapon stage ────────────────────────────────────────────
        CreateText(weaponPanel.transform, "Title", "CHOOSE YOUR WEAPON", 56, new Vector2(0, 460), new Vector2(1200, 80), TextAlignmentOptions.Center, FontStyles.Bold);
        CreatorCarousel weaponCarousel = BuildCarousel(weaponPanel);
        Button weaponNext = CreateButton(weaponPanel.transform, "NextButton", "NEXT", new Vector2(760, -480), new Vector2(220, 70));
        Button weaponBack = CreateButton(weaponPanel.transform, "BackButton", "BACK", new Vector2(-760, -480), new Vector2(180, 70));

        // ── Pact stage ──────────────────────────────────────────────
        CreateText(pactPanel.transform, "Title", "SIGN A PACT  <size=60%>(optional)</size>", 56, new Vector2(0, 460), new Vector2(1200, 80), TextAlignmentOptions.Center, FontStyles.Bold);
        GameObject pactList = CreateUIObject(pactPanel.transform, "PactList");
        CenterRect(pactList, new Vector2(0, 20), new Vector2(760, 640));
        VerticalLayoutGroup vlg = pactList.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 15;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = false;  vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;
        Button pactNext = CreateButton(pactPanel.transform, "NextButton", "NEXT", new Vector2(760, -480), new Vector2(220, 70));
        Button pactBack = CreateButton(pactPanel.transform, "BackButton", "BACK", new Vector2(-760, -480), new Vector2(180, 70));

        // ── Name stage ──────────────────────────────────────────────
        CreateText(namePanel.transform, "Title", "NAME YOUR SINNER", 56, new Vector2(0, 460), new Vector2(1200, 80), TextAlignmentOptions.Center, FontStyles.Bold);
        TMP_InputField nameInput = CreateInputField(namePanel.transform, "NameInput", "Sinner", new Vector2(0, 60), new Vector2(600, 90));
        Button startButton = CreateButton(namePanel.transform, "StartButton", "START", new Vector2(0, -100), new Vector2(300, 90), 36);
        Button nameBack = CreateButton(namePanel.transform, "BackButton", "BACK", new Vector2(-760, -480), new Vector2(180, 70));

        // ── Controller ──────────────────────────────────────────────
        CharacterCreator creator = canvas.gameObject.AddComponent<CharacterCreator>();
        creator.classPanel      = classPanel;
        creator.weaponPanel     = weaponPanel;
        creator.pactPanel       = pactPanel;
        creator.namePanel       = namePanel;
        creator.classCarousel   = classCarousel;
        creator.weaponCarousel  = weaponCarousel;
        creator.classNextButton = classNext;
        creator.weaponNextButton = weaponNext;
        creator.weaponBackButton = weaponBack;
        creator.pactListContainer = pactList.transform;
        creator.pactOptionPrefab  = EnsurePactOptionPrefab();
        creator.pactNextButton  = pactNext;
        creator.pactBackButton  = pactBack;
        creator.nameInput       = nameInput;
        creator.startButton     = startButton;
        creator.nameBackButton  = nameBack;
        // Lists intentionally left empty — CharacterCreator auto-loads all
        // definitions from Resources at runtime, so new content just appears.

        weaponPanel.SetActive(false);
        pactPanel.SetActive(false);
        namePanel.SetActive(false);

        FinishScene(canvas.gameObject,
            "Character Creator UI built. If an old creator canvas exists, delete it. Assign class/weapon sprites on their assets.");
    }

    // ================================================================
    //  3. RUN SUMMARY UI
    // ================================================================

    [MenuItem("Tools/Go To Hell/3. Build Run Summary UI")]
    public static void BuildRunSummaryUI()
    {
        if (Object.FindFirstObjectByType<RunSummaryUI>(FindObjectsInactive.Include) != null)
        {
            Debug.LogWarning("[GoToHellSetup] A RunSummaryUI already exists in this scene.");
            return;
        }

        Canvas canvas = CreateCanvas("RunSummaryCanvas", 200);
        Transform root = canvas.transform;

        GameObject panel = CreateUIObject(root, "SummaryPanel");
        StretchRect(panel);
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.88f);

        TMP_Text title = CreateText(panel.transform, "Title", "YOU DIED", 80, new Vector2(0, 340), new Vector2(1200, 110), TextAlignmentOptions.Center, FontStyles.Bold);
        TMP_Text stats = CreateText(panel.transform, "Stats", "", 28, new Vector2(0, -30), new Vector2(720, 540), TextAlignmentOptions.Center);
        Button menuBtn = CreateButton(panel.transform, "MenuButton", "BACK TO MENU", new Vector2(0, -420), new Vector2(320, 80), 30);

        RunSummaryUI summary = canvas.gameObject.AddComponent<RunSummaryUI>();
        summary.panel      = panel;
        summary.titleText  = title;
        summary.statsText  = stats;
        summary.menuButton = menuBtn;

        panel.SetActive(false);

        FinishScene(canvas.gameObject, "Run Summary UI built (panel hidden until death/victory).");
    }

    // ================================================================
    //  4. COLLECTION UI
    // ================================================================

    [MenuItem("Tools/Go To Hell/4. Build Collection UI")]
    public static void BuildCollectionUI()
    {
        if (GameObject.Find("CollectionCanvas") != null)
        {
            Debug.LogWarning("[GoToHellSetup] 'CollectionCanvas' already exists in this scene — delete it first to rebuild.");
            return;
        }

        Canvas canvas = CreateCanvas("CollectionCanvas", 10);
        Transform root = canvas.transform;

        CreateText(root, "Title", "COLLECTION", 56, new Vector2(0, 470), new Vector2(800, 80), TextAlignmentOptions.Center, FontStyles.Bold);
        Button backBtn = CreateButton(root, "BackButton", "BACK", new Vector2(-850, 470), new Vector2(160, 60));

        Button classesTab     = CreateButton(root, "ClassesTab",     "CLASSES",     new Vector2(-600, 380), new Vector2(270, 60));
        Button weaponsTab     = CreateButton(root, "WeaponsTab",     "WEAPONS",     new Vector2(-300, 380), new Vector2(270, 60));
        Button pactsTab       = CreateButton(root, "PactsTab",       "PACTS",       new Vector2(0, 380),    new Vector2(270, 60));
        Button upgradesTab    = CreateButton(root, "UpgradesTab",    "UPGRADES",    new Vector2(300, 380),  new Vector2(270, 60));
        Button leaderboardTab = CreateButton(root, "LeaderboardTab", "LEADERBOARD", new Vector2(600, 380),  new Vector2(270, 60), 24);

        // Scroll view
        GameObject scroll = CreateUIObject(root, "Scroll");
        CenterRect(scroll, new Vector2(0, -90), new Vector2(1500, 680));
        Image scrollBg = scroll.AddComponent<Image>();
        scrollBg.color = new Color(1f, 1f, 1f, 0.03f);
        ScrollRect scrollRect = scroll.AddComponent<ScrollRect>();

        GameObject viewport = CreateUIObject(scroll.transform, "Viewport");
        StretchRect(viewport);
        viewport.AddComponent<RectMask2D>();

        GameObject content = CreateUIObject(viewport.transform, "Content");
        RectTransform crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 1f);
        crt.anchorMax = new Vector2(0.5f, 1f);
        crt.pivot     = new Vector2(0.5f, 1f);
        crt.sizeDelta = new Vector2(1500, 0);
        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(180, 220);
        grid.spacing  = new Vector2(20, 20);
        grid.padding  = new RectOffset(20, 20, 20, 20);
        grid.childAlignment = TextAnchor.UpperCenter;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content  = crt;
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 30f;

        // ── Leaderboard panel (own scroll view, hidden until its tab is clicked)
        GameObject lbPanel = CreateUIObject(root, "LeaderboardPanel");
        CenterRect(lbPanel, new Vector2(0, -90), new Vector2(1500, 680));
        Image lbBg = lbPanel.AddComponent<Image>();
        lbBg.color = new Color(1f, 1f, 1f, 0.03f);
        ScrollRect lbScrollRect = lbPanel.AddComponent<ScrollRect>();

        GameObject lbViewport = CreateUIObject(lbPanel.transform, "Viewport");
        StretchRect(lbViewport);
        lbViewport.AddComponent<RectMask2D>();

        GameObject lbContent = CreateUIObject(lbViewport.transform, "Content");
        RectTransform lbRt = lbContent.GetComponent<RectTransform>();
        lbRt.anchorMin = new Vector2(0f, 1f);
        lbRt.anchorMax = new Vector2(1f, 1f);
        lbRt.pivot     = new Vector2(0.5f, 1f);
        lbRt.offsetMin = new Vector2(40f, 0f);
        lbRt.offsetMax = new Vector2(-40f, 0f);
        TextMeshProUGUI lbText = lbContent.AddComponent<TextMeshProUGUI>();
        lbText.fontSize = 28;
        lbText.alignment = TextAlignmentOptions.TopLeft;
        lbText.color = Color.white;
        lbText.lineSpacing = 14f;
        ContentSizeFitter lbFitter = lbContent.AddComponent<ContentSizeFitter>();
        lbFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        lbScrollRect.viewport = lbViewport.GetComponent<RectTransform>();
        lbScrollRect.content  = lbRt;
        lbScrollRect.horizontal = false;
        lbScrollRect.scrollSensitivity = 30f;
        lbPanel.SetActive(false);

        CollectionUI collection = canvas.gameObject.AddComponent<CollectionUI>();
        collection.gridScroll           = scroll;
        collection.contentContainer     = content.transform;
        collection.itemPrefab           = EnsureCollectionItemPrefab();
        collection.leaderboardPanel     = lbPanel;
        collection.leaderboardText      = lbText;
        collection.classesTabButton     = classesTab;
        collection.weaponsTabButton     = weaponsTab;
        collection.pactsTabButton       = pactsTab;
        collection.upgradesTabButton    = upgradesTab;
        collection.leaderboardTabButton = leaderboardTab;
        collection.backButton           = backBtn;

        FinishScene(canvas.gameObject, "Collection UI built (incl. Leaderboard tab).");
    }

    // ================================================================
    //  PREFAB BUILDERS
    // ================================================================

    private static GameObject EnsurePactOptionPrefab()
    {
        const string path = "Assets/Prefabs/UI/PactOption.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject rootGo = CreateUIObject(null, "PactOption");
        RectTransform rt = rootGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(760, 120);
        Image bg = rootGo.AddComponent<Image>();
        bg.color = new Color(0.13f, 0.12f, 0.16f, 1f);
        Button button = rootGo.AddComponent<Button>();
        button.targetGraphic = bg;

        GameObject highlight = CreateUIObject(rootGo.transform, "Highlight");
        StretchRect(highlight);
        Image hlImg = highlight.AddComponent<Image>();
        hlImg.color = new Color(0.9f, 0.7f, 0.2f, 0.25f);
        hlImg.raycastTarget = false;
        highlight.SetActive(false);

        GameObject iconGo = CreateUIObject(rootGo.transform, "Icon");
        CenterRect(iconGo, new Vector2(-330, 0), new Vector2(90, 90));
        Image icon = iconGo.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        TMP_Text nameLabel = CreateText(rootGo.transform, "Name", "Pact Name", 26,
            new Vector2(60, 28), new Vector2(560, 40), TextAlignmentOptions.Left, FontStyles.Bold);
        TMP_Text descLabel = CreateText(rootGo.transform, "Description", "Description", 18,
            new Vector2(60, -22), new Vector2(560, 62), TextAlignmentOptions.TopLeft);

        PactOptionButton opt = rootGo.AddComponent<PactOptionButton>();
        opt.nameLabel = nameLabel;
        opt.descriptionLabel = descLabel;
        opt.iconImage = icon;
        opt.button = button;
        opt.selectedHighlight = highlight;

        EnsureFolder("Assets/Prefabs/UI");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rootGo, path);
        Object.DestroyImmediate(rootGo);
        Debug.Log("[GoToHellSetup] Created " + path);
        return prefab;
    }

    private static GameObject EnsureCollectionItemPrefab()
    {
        const string path = "Assets/Prefabs/UI/CollectionItem.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject rootGo = CreateUIObject(null, "CollectionItem");
        rootGo.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 220);
        Image bg = rootGo.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.14f, 1f);

        GameObject iconGo = CreateUIObject(rootGo.transform, "Icon");
        CenterRect(iconGo, new Vector2(0, 30), new Vector2(140, 140));
        Image icon = iconGo.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        TMP_Text nameText = CreateText(rootGo.transform, "Name", "Name", 20,
            new Vector2(0, -80), new Vector2(170, 52), TextAlignmentOptions.Center);

        GameObject overlay = CreateUIObject(rootGo.transform, "LockedOverlay");
        StretchRect(overlay);
        Image ovImg = overlay.AddComponent<Image>();
        ovImg.color = new Color(0f, 0f, 0f, 0.55f);
        ovImg.raycastTarget = false;
        CreateText(overlay.transform, "Question", "?", 64, new Vector2(0, 20), new Vector2(100, 100), TextAlignmentOptions.Center, FontStyles.Bold);
        overlay.SetActive(false);

        CollectionItemUI item = rootGo.AddComponent<CollectionItemUI>();
        item.iconImage = icon;
        item.nameText = nameText;
        item.lockedOverlay = overlay;

        EnsureFolder("Assets/Prefabs/UI");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rootGo, path);
        Object.DestroyImmediate(rootGo);
        Debug.Log("[GoToHellSetup] Created " + path);
        return prefab;
    }

    // ================================================================
    //  CAROUSEL BUILDER
    // ================================================================

    private static CreatorCarousel BuildCarousel(GameObject panel)
    {
        // Side slots first so the center renders on top ("to the back" look)
        Image left = CreateImageSlot(panel.transform, "LeftSlot", new Vector2(-340, 80), new Vector2(210, 300));
        Image right = CreateImageSlot(panel.transform, "RightSlot", new Vector2(340, 80), new Vector2(210, 300));
        Image center = CreateImageSlot(panel.transform, "CenterSlot", new Vector2(0, 60), new Vector2(320, 440));

        Button leftArrow = CreateButton(panel.transform, "LeftArrow", "<", new Vector2(-560, 60), new Vector2(80, 80), 40);
        Button rightArrow = CreateButton(panel.transform, "RightArrow", ">", new Vector2(560, 60), new Vector2(80, 80), 40);

        TMP_Text nameText = CreateText(panel.transform, "Name", "", 44,
            new Vector2(0, -220), new Vector2(900, 60), TextAlignmentOptions.Center, FontStyles.Bold);
        TMP_Text descText = CreateText(panel.transform, "Description", "", 22,
            new Vector2(0, -350), new Vector2(820, 200), TextAlignmentOptions.Top);

        CreatorCarousel carousel = panel.AddComponent<CreatorCarousel>();
        carousel.centerImage = center;
        carousel.leftImage = left;
        carousel.rightImage = right;
        carousel.leftArrow = leftArrow;
        carousel.rightArrow = rightArrow;
        carousel.nameText = nameText;
        carousel.descriptionText = descText;
        return carousel;
    }

    // ================================================================
    //  UI HELPERS
    // ================================================================

    private static Canvas CreateCanvas(string name, int sortOrder)
    {
        GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        if (Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        return canvas;
    }

    private static GameObject CreateUIObject(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject CreatePanel(Transform parent, string name)
    {
        GameObject go = CreateUIObject(parent, name);
        StretchRect(go);
        return go;
    }

    private static void StretchRect(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void CenterRect(GameObject go, Vector2 pos, Vector2 size)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size,
        Vector2 pos, Vector2 dim, TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
    {
        GameObject go = CreateUIObject(parent, name);
        CenterRect(go, pos, dim);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Image CreateImageSlot(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = CreateUIObject(parent, name);
        CenterRect(go, pos, size);
        Image img = go.AddComponent<Image>();
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    private static Button CreateButton(Transform parent, string name, string label,
        Vector2 pos, Vector2 size, float fontSize = 28)
    {
        GameObject go = CreateUIObject(parent, name);
        CenterRect(go, pos, size);
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.16f, 0.15f, 0.2f, 1f);
        Button button = go.AddComponent<Button>();
        button.targetGraphic = bg;

        TextMeshProUGUI text = CreateText(go.transform, "Label", label, fontSize, Vector2.zero, size, TextAlignmentOptions.Center, FontStyles.Bold);
        StretchRect(text.gameObject);
        return button;
    }

    private static TMP_InputField CreateInputField(Transform parent, string name, string placeholderText,
        Vector2 pos, Vector2 size)
    {
        GameObject go = CreateUIObject(parent, name);
        CenterRect(go, pos, size);
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.13f, 1f);
        TMP_InputField field = go.AddComponent<TMP_InputField>();
        field.targetGraphic = bg;

        GameObject area = CreateUIObject(go.transform, "Text Area");
        StretchRect(area);
        RectTransform art = area.GetComponent<RectTransform>();
        art.offsetMin = new Vector2(15, 8);
        art.offsetMax = new Vector2(-15, -8);
        area.AddComponent<RectMask2D>();

        TextMeshProUGUI placeholder = CreateText(area.transform, "Placeholder", placeholderText, 32, Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
        StretchRect(placeholder.gameObject);
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.color = new Color(1f, 1f, 1f, 0.35f);

        TextMeshProUGUI text = CreateText(area.transform, "Text", "", 32, Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
        StretchRect(text.gameObject);

        field.textViewport = art;
        field.textComponent = text;
        field.placeholder = placeholder;
        return field;
    }

    // ================================================================
    //  MISC HELPERS
    // ================================================================

    private static List<T> LoadAllAssets<T>() where T : Object =>
        AssetDatabase.FindAssets("t:" + typeof(T).Name)
            .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(a => a != null)
            .ToList();

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0, slash);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
    }

    private static void FinishScene(GameObject select, string message)
    {
        Selection.activeGameObject = select;
        EditorSceneManager.MarkSceneDirty(select.scene);
        Debug.Log("[GoToHellSetup] " + message + " Save the scene (Ctrl+S).");
    }
}
