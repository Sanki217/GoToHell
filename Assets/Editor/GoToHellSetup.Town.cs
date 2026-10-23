using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Classes, weapons, class upgrades and the Town hub (Tools → Go To Hell → …).
///
///   1. Create Default Assets          → also runs SetupClassesAndWeapons below
///   Characters/Sync Kit Into Class Prefabs → kick + class skills + pact skills on every class prefab
///   Town/Build Town Scene             → creates Assets/Scenes/Town.unity (stations, HUD, panels, pause)
///
/// Everything is safe to re-run: existing assets keep their tuning, only
/// missing pieces are filled in.
/// </summary>
public static partial class GoToHellSetup
{
    // ================================================================
    //  CLASSES + WEAPONS
    // ================================================================

    private static void SetupClassesAndWeapons(ClassDefinition rogue, ClassDefinition warrior,
                                               WeaponDefinition bow, WeaponDefinition sword)
    {
        ClassDefinition priest = EnsurePriest(rogue);

        ConfigureClass(rogue, bow, "SmokeBombSkill", "Smoke Bomb",
            "Vanish in smoke: you can't be hurt for a moment, enemies in the cloud are slowed and your next arrow crits.",
            "Cutthroat", "+10% critical hit chance.", new CritChanceEffect { bonus = 0.1f });

        ConfigureClass(warrior, sword, "WarCrySkill", "War Cry",
            "A shout that damages, knocks back and staggers every enemy around you.",
            "Iron Hide", "Take 15% less damage.", new DamageTakenEffect { multiplier = 0.85f });

        ConfigureClass(priest, bow, "HolyNovaSkill", "Holy Nova",
            "A burst of light: heal yourself and smite every enemy around you with Holy.",
            "Devotion", "Heal 1 HP for every soul you collect.", new HealOnSoulEffect { hpPerSoul = 1 });

        ConfigureWeaponLevels(bow,
            ("+2 Attack Damage", 60, new PrimaryStatEffect { stat = PrimaryStat.AttackDamage, amount = 2 }),
            ("+1 arrow in the quiver", 120, new ExtraArrowsEffect { arrows = 1 }),
            ("Arrows pierce one enemy", 220, new ArrowPierceEffect { pierce = 1 }));
        ConfigureWeaponLevels(sword,
            ("+2 Attack Damage", 60, new PrimaryStatEffect { stat = PrimaryStat.AttackDamage, amount = 2 }),
            ("+2 Size — longer reach", 120, new PrimaryStatEffect { stat = PrimaryStat.Size, amount = 2 }),
            ("+3 Cooldown — faster swings", 220, new PrimaryStatEffect { stat = PrimaryStat.Cooldown, amount = 3 }));

        EnsureProgressionConfig();
        CreateClassUpgrades();
        Content.InvalidateAll();
    }

    private static ClassDefinition EnsurePriest(ClassDefinition rogue)
    {
        const string path = "Assets/Resources/Classes/Priest.asset";
        ClassDefinition priest = AssetDatabase.LoadAssetAtPath<ClassDefinition>(path);
        if (priest == null)
        {
            priest = ScriptableObject.CreateInstance<ClassDefinition>();
            priest.classId = "priest";
            priest.displayName = "Priest";
            priest.description = "A fallen cleric still clinging to a little light. Heals through the dark.";
            priest.unlockedByDefault = true;
            AssetDatabase.CreateAsset(priest, path);
            Debug.Log("[GoToHellSetup] Created Priest class (assign its sprite in the Inspector).");
        }

        // Own player prefab (a copy of the Rogue's) so the Priest can get its own look later
        if (priest.playerPrefab == null && rogue != null && rogue.playerPrefab != null)
        {
            string roguePrefabPath = AssetDatabase.GetAssetPath(rogue.playerPrefab);
            string priestPrefabPath = Path.Combine(Path.GetDirectoryName(roguePrefabPath) ?? "Assets", "Priest.prefab").Replace('\\', '/');
            if (AssetDatabase.LoadAssetAtPath<GameObject>(priestPrefabPath) == null)
                AssetDatabase.CopyAsset(roguePrefabPath, priestPrefabPath);
            priest.playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(priestPrefabPath);
            Debug.Log($"[GoToHellSetup] Priest player prefab: {priestPrefabPath} (copy of the Rogue's — restyle freely).");
        }
        EditorUtility.SetDirty(priest);
        return priest;
    }

    /// <summary>Fills empty class fields — never overwrites your tuning.</summary>
    private static void ConfigureClass(ClassDefinition cls, WeaponDefinition startingWeapon,
        string skillComponent, string skillName, string skillDescription,
        string passiveName, string passiveDescription, RunEffect passive)
    {
        if (cls == null) return;
        if (cls.startingWeapon == null) cls.startingWeapon = startingWeapon;
        if (string.IsNullOrEmpty(cls.activeSkillComponentName)) cls.activeSkillComponentName = skillComponent;
        if (string.IsNullOrEmpty(cls.activeSkillName)) cls.activeSkillName = skillName;
        if (string.IsNullOrEmpty(cls.activeSkillDescription)) cls.activeSkillDescription = skillDescription;
        if (string.IsNullOrEmpty(cls.passiveName)) cls.passiveName = passiveName;
        if (string.IsNullOrEmpty(cls.passiveDescription)) cls.passiveDescription = passiveDescription;
        cls.passiveEffects ??= new List<RunEffect>();
        cls.passiveEffects.RemoveAll(e => e == null);
        if (cls.passiveEffects.Count == 0) cls.passiveEffects.Add(passive);
        EditorUtility.SetDirty(cls);
    }

    private static void ConfigureWeaponLevels(WeaponDefinition w, params (string description, int cost, RunEffect effect)[] levels)
    {
        if (w == null) return;
        w.levels ??= new List<WeaponDefinition.WeaponLevel>();
        if (w.levels.Count > 0) return;   // already authored
        foreach (var (description, cost, effect) in levels)
            w.levels.Add(new WeaponDefinition.WeaponLevel
            {
                description = description,
                cost = cost,
                effects = new List<RunEffect> { effect }
            });
        EditorUtility.SetDirty(w);
        Debug.Log($"[GoToHellSetup] {w.displayName}: {levels.Length} smith levels added.");
    }

    private static void EnsureProgressionConfig()
    {
        string path = $"Assets/Resources/{CharacterProgressionConfig.ResourcePath}.asset";
        if (AssetDatabase.LoadAssetAtPath<CharacterProgressionConfig>(path) != null) return;
        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<CharacterProgressionConfig>(), path);
        Debug.Log($"[GoToHellSetup] Created {path} (character XP / level tuning).");
    }

    // ================================================================
    //  CLASS UPGRADES (run upgrades only one class can find)
    // ================================================================

    private struct ClassUpgradeSpec
    {
        public System.Type type;
        public string file, name, description, classId;
        public UpgradeTag tags;
    }

    private static void CreateClassUpgrades()
    {
        PlayerUpgradePool pool = Resources.Load<PlayerUpgradePool>("UpgradePool");
        if (pool == null)
        {
            Debug.LogWarning("[GoToHellSetup] No Resources/UpgradePool — class upgrades skipped.");
            return;
        }

        var specs = new[]
        {
            new ClassUpgradeSpec { type = typeof(UpgradeThunderousCry), file = "Thunderous Cry", name = "Thunderous Cry",
                description = "War Cry reaches further and staggers longer.", classId = "warrior", tags = UpgradeTag.Control },
            new ClassUpgradeSpec { type = typeof(UpgradeLingeringSmoke), file = "Lingering Smoke", name = "Lingering Smoke",
                description = "Smoke Bomb lasts longer and recharges faster.", classId = "rogue", tags = UpgradeTag.Defense },
            new ClassUpgradeSpec { type = typeof(UpgradeRadiance), file = "Radiance", name = "Radiance",
                description = "Holy Nova heals and smites harder over a wider area.", classId = "priest", tags = UpgradeTag.Holy | UpgradeTag.Sustain },
        };

        GameObject template = pool.upgradePrefabs.FirstOrDefault(p => p != null && p.GetComponent<PlayerUpgrade>() != null);
        if (template == null)
        {
            Debug.LogWarning("[GoToHellSetup] UpgradePool has no upgrade prefab to copy — class upgrades skipped.");
            return;
        }
        string templatePath = AssetDatabase.GetAssetPath(template);
        string folder = Path.GetDirectoryName(templatePath)?.Replace('\\', '/') ?? "Assets";

        int made = 0;
        foreach (ClassUpgradeSpec spec in specs)
        {
            if (pool.upgradePrefabs.Any(p => p != null && p.GetComponent(spec.type) != null)) continue;

            string path = $"{folder}/{spec.file}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                AssetDatabase.CopyAsset(templatePath, path);
                using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
                {
                    GameObject root = scope.prefabContentsRoot;
                    root.name = spec.file;
                    Sprite icon = null;
                    foreach (PlayerUpgrade old in root.GetComponents<PlayerUpgrade>())
                    {
                        icon = old.icon;
                        Object.DestroyImmediate(old, true);
                    }
                    var u = (PlayerUpgrade)root.AddComponent(spec.type);
                    u.displayName = spec.name;
                    u.description = spec.description;
                    u.icon = icon;
                    u.category = UpgradeCategory.Passive;
                    u.rarity = UpgradeRarity.Rare;
                    u.tags = spec.tags;
                    u.requiredClass = spec.classId;
                    u.requiredWeaponId = "";
                    u.requiredPactId = "";
                }
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            pool.upgradePrefabs.Add(prefab);
            made++;
            Debug.Log($"[GoToHellSetup] Class upgrade '{spec.name}' ({spec.classId}) → {path}, added to UpgradePool.");
        }
        if (made > 0) EditorUtility.SetDirty(pool);
    }

    // ================================================================
    //  KIT SYNC — kick + class skills on every class prefab
    // ================================================================

    private static int AddKitComponents(List<GameObject> prefabs)
    {
        var types = new List<System.Type> { typeof(PlayerKick) };
        foreach (ClassDefinition c in LoadAllAssets<ClassDefinition>())
        {
            if (string.IsNullOrEmpty(c.activeSkillComponentName)) continue;
            System.Type t = TypeCache.GetTypesDerivedFrom<ClassSkill>().FirstOrDefault(x => x.Name == c.activeSkillComponentName);
            if (t == null) Debug.LogWarning($"[GoToHellSetup] Class '{c.classId}': no ClassSkill type '{c.activeSkillComponentName}'.");
            else if (!types.Contains(t)) types.Add(t);
        }

        int added = 0;
        foreach (GameObject target in prefabs)
        {
            List<System.Type> missing = types.Where(t => target.GetComponent(t) == null).ToList();
            if (missing.Count == 0) continue;

            using (var scope = new PrefabUtility.EditPrefabContentsScope(AssetDatabase.GetAssetPath(target)))
            {
                foreach (System.Type t in missing)
                {
                    Component comp = scope.prefabContentsRoot.AddComponent(t);
                    if (comp is ClassSkill skill)
                    {
                        ApplySkillDefaults(skill);
                        skill.enabled = false;   // RunConfigApplier enables the class's own
                    }
                    added++;
                    Debug.Log($"[GoToHellSetup] Added {t.Name} to {target.name}.");
                }
            }
        }
        return added;
    }

    private static void ApplySkillDefaults(ClassSkill skill)
    {
        switch (skill)
        {
            case WarCrySkill w:
                w.skillName = "War Cry"; w.baseCooldown = 10f; w.effectColor = new Color(1f, 0.35f, 0.2f, 0.35f); break;
            case SmokeBombSkill s:
                s.skillName = "Smoke Bomb"; s.baseCooldown = 12f; s.effectColor = new Color(0.6f, 0.6f, 0.65f, 0.45f); break;
            case HolyNovaSkill h:
                h.skillName = "Holy Nova"; h.baseCooldown = 14f; h.effectColor = new Color(1f, 0.9f, 0.45f, 0.4f); break;
        }
    }

    // ================================================================
    //  TOWN SCENE
    // ================================================================

    private const string TownScenePath = "Assets/Scenes/Town.unity";

    [MenuItem("Tools/Go To Hell/Town/Build Town Scene")]
    public static void BuildTownScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TownScenePath) != null)
        {
            Debug.LogWarning($"[GoToHellSetup] {TownScenePath} already exists — delete it first to rebuild, " +
                             "or open it and edit by hand.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ---- Camera ----
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 3.5f, -16f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.07f, 0.07f);
            cam.gameObject.AddComponent<TownCamera>();
        }

        // ---- Street ----
        const float halfLength = 40f;
        GameObject street = new GameObject("Street");
        Block(street.transform, "Ground", new Vector3(0f, -0.5f, 0f), new Vector3(halfLength * 2f + 2f, 1f, 6f), Layers.Ground);
        Block(street.transform, "Wall Left", new Vector3(-halfLength - 0.5f, 9.5f, 0f), new Vector3(1f, 20f, 6f), Layers.Wall);
        Block(street.transform, "Wall Right", new Vector3(halfLength + 0.5f, 9.5f, 0f), new Vector3(1f, 20f, 6f), Layers.Wall);
        Block(street.transform, "Backdrop", new Vector3(0f, 6f, 4f), new Vector3(halfLength * 2f, 14f, 0.5f), -1);
        TownCamera tc = cam != null ? cam.GetComponent<TownCamera>() : null;
        if (tc != null) { tc.minX = -halfLength + 12f; tc.maxX = halfLength - 12f; }

        // ---- Player ----
        ClassDefinition anyClass = LoadAllAssets<ClassDefinition>().FirstOrDefault(c => c.playerPrefab != null);
        PlayerSpawner spawner = new GameObject("PlayerSpawner").AddComponent<PlayerSpawner>();
        spawner.transform.position = new Vector3(0f, 1.2f, 0f);
        spawner.defaultPlayerPrefab = anyClass != null ? anyClass.playerPrefab : null;

        TownManager manager = new GameObject("TownManager").AddComponent<TownManager>();
        new GameObject("UIScaleManager").AddComponent<UIScaleManager>();
        new GameObject("DebugOverlay").AddComponent<DebugOverlay>();

        // ---- UI ----
        Canvas canvas = CreateCanvas("TownCanvas", 50);
        Transform root = canvas.transform;
        DefaultControls.Resources res = UiResources();

        TownHUD hud = canvas.gameObject.AddComponent<TownHUD>();
        hud.nameText = CreateText(root, "Name", "Name — Class", 32, Vector2.zero, new Vector2(900, 50), TextAlignmentOptions.Left, FontStyles.Bold);
        Anchor(hud.nameText.gameObject, new Vector2(0, 1), new Vector2(480, -45));
        hud.levelText = CreateText(root, "Level", "Level 1", 26, Vector2.zero, new Vector2(900, 40), TextAlignmentOptions.Left);
        Anchor(hud.levelText.gameObject, new Vector2(0, 1), new Vector2(480, -90));
        GameObject xp = DefaultControls.CreateSlider(res);
        xp.name = "XpBar";
        xp.transform.SetParent(root, false);
        Anchor(xp, new Vector2(0, 1), new Vector2(230, -125));
        ((RectTransform)xp.transform).sizeDelta = new Vector2(400, 20);
        hud.xpBar = xp.GetComponent<Slider>();
        hud.xpBar.interactable = false;
        Transform handle = xp.transform.Find("Handle Slide Area");
        if (handle != null) Object.DestroyImmediate(handle.gameObject);
        hud.currencyText = CreateText(root, "Currency", "0 Brimstone", 32, Vector2.zero, new Vector2(500, 50), TextAlignmentOptions.Right, FontStyles.Bold);
        Anchor(hud.currencyText.gameObject, new Vector2(1, 1), new Vector2(-280, -45));

        TownDeparture depart = canvas.gameObject.AddComponent<TownDeparture>();
        depart.hintText = CreateText(root, "DescendHint", "Hold R to descend", 26, Vector2.zero, new Vector2(700, 40), TextAlignmentOptions.Center);
        Anchor(depart.hintText.gameObject, new Vector2(0.5f, 0), new Vector2(0, 90));
        GameObject fillGo = CreateUIObject(root, "DescendProgress");
        Anchor(fillGo, new Vector2(0.5f, 0), new Vector2(0, 55));
        ((RectTransform)fillGo.transform).sizeDelta = new Vector2(400, 14);
        Image fill = fillGo.AddComponent<Image>();
        fill.sprite = res.standard;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.raycastTarget = false;
        depart.progressFill = fill;

        // Weapon rack panel
        WeaponRackUI rack = canvas.gameObject.AddComponent<WeaponRackUI>();
        rack.panel = CreateModal(root, "WeaponRackPanel", out Transform rw);
        CreateText(rw, "Header", "WEAPON RACK", 48, new Vector2(0, 400), new Vector2(1400, 70), TextAlignmentOptions.Center, FontStyles.Bold);
        rack.content = CreateScrollList(rw, new Vector2(-330, 0), new Vector2(760, 720));
        rack.rowTemplate = RowTemplate(rack.content);
        rack.detailText = CreateText(rw, "Detail", "", 28, new Vector2(430, 80), new Vector2(620, 560), TextAlignmentOptions.TopLeft);
        rack.equipButton = CreateButton(rw, "Equip", "EQUIP", new Vector2(430, -290), new Vector2(420, 90), 36);
        rack.closeButton = CreateButton(rw, "Close", "CLOSE", new Vector2(600, -420), new Vector2(220, 64), 24);

        // Smith panel
        SmithUI smith = canvas.gameObject.AddComponent<SmithUI>();
        smith.panel = CreateModal(root, "SmithPanel", out Transform sw);
        CreateText(sw, "Header", "SMITH", 48, new Vector2(0, 400), new Vector2(1400, 70), TextAlignmentOptions.Center, FontStyles.Bold);
        smith.titleText = CreateText(sw, "Weapon", "", 36, new Vector2(0, 300), new Vector2(1400, 60), TextAlignmentOptions.Center);
        smith.levelsText = CreateText(sw, "Levels", "", 28, new Vector2(0, 20), new Vector2(1300, 460), TextAlignmentOptions.TopLeft);
        smith.currencyText = CreateText(sw, "Currency", "", 30, new Vector2(-430, -300), new Vector2(600, 60), TextAlignmentOptions.Left, FontStyles.Bold);
        smith.upgradeButton = CreateButton(sw, "Upgrade", "UPGRADE", new Vector2(430, -290), new Vector2(420, 90), 32);
        smith.upgradeButtonLabel = smith.upgradeButton.GetComponentInChildren<TMP_Text>();
        smith.closeButton = CreateButton(sw, "Close", "CLOSE", new Vector2(600, -420), new Vector2(220, 64), 24);

        // Shrine = meta shop
        MetaShopUI shop = canvas.gameObject.AddComponent<MetaShopUI>();
        shop.panel = CreateModal(root, "ShrinePanel", out Transform mw);
        CreateText(mw, "Header", "SHRINE OF BRIMSTONE", 48, new Vector2(-200, 400), new Vector2(1000, 70), TextAlignmentOptions.Left, FontStyles.Bold);
        shop.currencyText = CreateText(mw, "Currency", "0", 36, new Vector2(400, 400), new Vector2(600, 70), TextAlignmentOptions.Right, FontStyles.Bold);
        shop.content = CreateScrollList(mw, new Vector2(-330, -10), new Vector2(760, 700));
        shop.rowTemplate = RowTemplate(shop.content);
        shop.detailText = CreateText(mw, "Detail", "", 28, new Vector2(430, 60), new Vector2(620, 560), TextAlignmentOptions.TopLeft);
        shop.buyButton = CreateButton(mw, "Buy", "BUY", new Vector2(430, -300), new Vector2(420, 80), 30);
        shop.buyButtonLabel = shop.buyButton.GetComponentInChildren<TMP_Text>();
        shop.refundButton = CreateButton(mw, "Refund", "REFUND ALL", new Vector2(-560, -420), new Vector2(260, 60), 22);
        shop.closeButton = CreateButton(mw, "Close", "DONE", new Vector2(600, -420), new Vector2(220, 64), 24);

        // Pact select (shown when leaving)
        PactSelectUI pacts = canvas.gameObject.AddComponent<PactSelectUI>();
        pacts.panel = CreateModal(root, "PactSelectPanel", out Transform pw);
        pacts.headerText = CreateText(pw, "Header", "CHOOSE A PACT", 48, new Vector2(0, 400), new Vector2(1400, 70), TextAlignmentOptions.Center, FontStyles.Bold);
        pacts.content = CreateScrollList(pw, new Vector2(-330, 0), new Vector2(760, 720));
        pacts.rowTemplate = RowTemplate(pacts.content);
        pacts.detailText = CreateText(pw, "Detail", "", 28, new Vector2(430, 80), new Vector2(620, 560), TextAlignmentOptions.TopLeft);
        pacts.startButton = CreateButton(pw, "Descend", "DESCEND", new Vector2(430, -290), new Vector2(420, 90), 36);
        pacts.backButton = CreateButton(pw, "Back", "BACK", new Vector2(600, -420), new Vector2(220, 64), 24);
        manager.pactSelect = pacts;

        foreach (GameObject p in new[] { rack.panel, smith.panel, shop.panel, pacts.panel }) p.SetActive(false);
        ApplyDefaultLook(canvas.gameObject);
        depart.hintText.color = Color.white;   // HUD text (not on a panel) stays white
        hud.nameText.color = hud.levelText.color = hud.currencyText.color = Color.white;

        // ---- Stations (left → right) ----
        GameObject stations = new GameObject("Stations");
        Station(stations.transform, "Shrine", -26f, shop, new Color(0.55f, 0.25f, 0.6f));
        Station(stations.transform, "Weapon Rack", -12f, rack, new Color(0.45f, 0.35f, 0.25f));
        Station(stations.transform, "Smith", 12f, smith, new Color(0.35f, 0.35f, 0.4f));

        GameObject dummyPrefab = FindPrefabWith<TrainingDummy>();
        if (dummyPrefab != null)
        {
            var dummy = (GameObject)PrefabUtility.InstantiatePrefab(dummyPrefab);
            dummy.transform.position = new Vector3(24f, 1f, 0f);
        }
        else Debug.Log("[GoToHellSetup] No TrainingDummy prefab found — place one in the town by hand if you want it.");

        GameObject exit = new GameObject("Exit — Descend");
        exit.transform.SetParent(stations.transform, false);
        exit.transform.position = new Vector3(halfLength - 2f, 2f, 0f);
        BoxCollider exitCol = exit.AddComponent<BoxCollider>();
        exitCol.isTrigger = true;
        exitCol.size = new Vector3(2f, 4f, 6f);
        exit.AddComponent<TownExit>();
        WorldLabel(exit.transform, "Label", "EXIT — DESCEND", new Vector3(0f, 3f, 0f), 6f);

        // ---- Pause menu (+ settings) ----
        BuildTownPause();
        BuildSettingsPanel();   // puts settings into the pause canvas

        EditorSceneManager.SaveScene(scene, TownScenePath);
        AddSceneToBuild(TownScenePath);
        Selection.activeGameObject = manager.gameObject;
        Debug.Log($"[GoToHellSetup] Town built and saved to {TownScenePath} (added to Build Settings). " +
                  "Stations: Shrine, Weapon Rack, Smith, Training Dummy, Exit. Move/restyle everything freely.");
    }

    private static void BuildTownPause()
    {
        Canvas canvas = CreateCanvas("PauseCanvas", 120);
        PauseMenu pause = canvas.gameObject.AddComponent<PauseMenu>();
        pause.pausePanel = CreateModal(canvas.transform, "PausePanel", out Transform w);
        ((RectTransform)w).sizeDelta = new Vector2(600, 520);
        CreateText(w, "Header", "PAUSED", 48, new Vector2(0, 190), new Vector2(560, 70), TextAlignmentOptions.Center, FontStyles.Bold);
        pause.resumeButton = CreateButton(w, "Resume", "Resume", new Vector2(0, 70), new Vector2(400, 70), 30);
        pause.settingsButton = CreateButton(w, "Settings", "Settings", new Vector2(0, -20), new Vector2(400, 70), 30);
        pause.quitToMenuButton = CreateButton(w, "QuitToMenu", "Quit to Menu", new Vector2(0, -110), new Vector2(400, 70), 30);
        pause.pausePanel.SetActive(false);
        ApplyDefaultLook(canvas.gameObject);
    }

    private static void Block(Transform parent, string name, Vector3 pos, Vector3 size, int layer)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = size;
        if (layer >= 0) go.layer = layer;
        else Object.DestroyImmediate(go.GetComponent<Collider>());   // visual only
    }

    private static void Station(Transform parent, string name, float x, MonoBehaviour panel, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, 1.5f, 0f);

        BoxCollider trigger = go.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(4f, 3f, 6f);

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(go.transform, false);
        visual.transform.localPosition = new Vector3(0f, 0f, 2f);
        visual.transform.localScale = new Vector3(3f, 3f, 1.5f);
        var mat = new Material(Shader.Find("Standard")) { color = color };
        string matFolder = "Assets/Materials/Town";
        EnsureFolder(matFolder);
        string matPath = $"{matFolder}/{name}.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(matPath) == null) AssetDatabase.CreateAsset(mat, matPath);
        visual.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        WorldLabel(go.transform, "Name", name.ToUpper(), new Vector3(0f, 2.2f, 0f), 5f);
        TownStation station = go.AddComponent<TownStation>();
        station.stationName = name;
        station.panel = panel;
        station.prompt = WorldLabel(go.transform, "Prompt", $"[E] {name}", new Vector3(0f, 3.2f, 0f), 4f);
        station.prompt.color = new Color(1f, 0.85f, 0.4f);
        station.prompt.gameObject.SetActive(false);
    }

    private static TMP_Text WorldLabel(Transform parent, string name, string text, Vector3 localPos, float size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        TextMeshPro t = go.AddComponent<TextMeshPro>();
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.rectTransform.sizeDelta = new Vector2(10f, 2f);
        t.color = Color.white;
        return t;
    }

    private static GameObject FindPrefabWith<T>() where T : Component
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (p != null && p.GetComponent<T>() != null) return p;
        }
        return null;
    }

    private static void AddSceneToBuild(string path)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == path)) return;
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
