using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Plain "programmer UI": Unity's stock sprites, colours and fonts.
///
///   UI/Apply Default Unity Look (menus in open scenes)
///       Restyles menus and screens IN PLACE — positions, sizes, elements and
///       wiring stay; only sprites, colours, fonts and text styling go back to
///       Unity defaults. Menu scenes (Main Menu, Collection) are done fully;
///       gameplay scenes only touch the pause, settings, run-summary and
///       name-prompt panels (HUD, level-up cards and chest UI are left alone).
///
///   UI/Build Settings Panel (open scene)
///       Tabbed SettingsUI made of default controls, wired into MainMenu or
///       PauseMenu (the old settings panel is hidden, not deleted).
/// </summary>
public static partial class GoToHellSetup
{
    private static readonly Color DefaultTextDark = new Color(0.196f, 0.196f, 0.196f, 1f);
    private static readonly Color DefaultPanel = new Color(1f, 1f, 1f, 0.392f);

    private static DefaultControls.Resources UiResources() => new DefaultControls.Resources
    {
        standard   = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
        background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
        inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
        knob       = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
        checkmark  = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
        dropdown   = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
        mask       = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd"),
    };

    private static TMP_DefaultControls.Resources TmpResources()
    {
        DefaultControls.Resources r = UiResources();
        return new TMP_DefaultControls.Resources
        {
            standard = r.standard, background = r.background, inputField = r.inputField,
            knob = r.knob, checkmark = r.checkmark, dropdown = r.dropdown, mask = r.mask
        };
    }

    // ================================================================
    //  RESTYLE IN PLACE
    // ================================================================

    [MenuItem("Tools/Go To Hell/UI/Apply Default Unity Look (menus in open scenes)")]
    public static void ApplyDefaultLookToMenus()
    {
        int roots = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            List<GameObject> targets = MenuRoots(scene);
            foreach (GameObject t in targets) ApplyDefaultLook(t);

            // Collection grid items are spawned from a prefab — restyle it too
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (CollectionUI c in root.GetComponentsInChildren<CollectionUI>(true))
                {
                    if (c.itemPrefab == null) continue;
                    string path = AssetDatabase.GetAssetPath(c.itemPrefab);
                    if (string.IsNullOrEmpty(path)) continue;
                    using (var edit = new PrefabUtility.EditPrefabContentsScope(path))
                        ApplyDefaultLook(edit.prefabContentsRoot);
                }
            if (targets.Count > 0) EditorSceneManager.MarkSceneDirty(scene);
            roots += targets.Count;
        }
        Debug.Log($"[GoToHellSetup] Default Unity look applied to {roots} menu root(s). Save the scene(s). " +
                  "Open the other scenes and run this again.");
    }

    /// <summary>Menu scenes: every screen-space canvas. Gameplay scenes: only the menu panels.</summary>
    private static List<GameObject> MenuRoots(Scene scene)
    {
        var list = new List<GameObject>();
        GameObject[] sceneRoots = scene.GetRootGameObjects();
        bool menuScene = sceneRoots.Any(r => r.GetComponentInChildren<MainMenu>(true) != null
                                          || r.GetComponentInChildren<CollectionUI>(true) != null);
        void Add(GameObject go) { if (go != null && !list.Contains(go)) list.Add(go); }

        foreach (GameObject root in sceneRoots)
        {
            if (menuScene)
            {
                foreach (Canvas c in root.GetComponentsInChildren<Canvas>(true))
                    if (c.renderMode != RenderMode.WorldSpace && c.GetComponentsInParent<Canvas>(true).Length == 1)
                        Add(c.gameObject);
                continue;
            }

            foreach (PauseMenu p in root.GetComponentsInChildren<PauseMenu>(true)) { Add(p.pausePanel); Add(p.settingsPanel); }
            foreach (RunSummaryUI r in root.GetComponentsInChildren<RunSummaryUI>(true)) Add(r.panel);
            foreach (NamePromptUI n in root.GetComponentsInChildren<NamePromptUI>(true)) Add(n.panel);
            foreach (SettingsUI s in root.GetComponentsInChildren<SettingsUI>(true)) Add(s.panel);
        }
        return list;
    }

    /// <summary>
    /// Stock Unity look for everything under <paramref name="root"/>: default
    /// sprites and colour tints on controls, default Panel on plain boxes,
    /// default TMP / legacy font with no bold, outlines or shadows. Images
    /// with real art (icons) are left alone.
    /// </summary>
    public static void ApplyDefaultLook(GameObject root)
    {
        if (root == null) return;
        DefaultControls.Resources res = UiResources();
        var controlGraphics = new HashSet<Graphic>();

        foreach (Selectable s in root.GetComponentsInChildren<Selectable>(true))
        {
            Undo.RecordObject(s, "Default look");
            s.transition = Selectable.Transition.ColorTint;
            s.colors = ColorBlock.defaultColorBlock;
            s.spriteState = default;

            switch (s)
            {
                case Toggle t:
                    StyleImage(t.targetGraphic as Image, res.standard, Color.white, controlGraphics);
                    StyleImage(t.graphic as Image, res.checkmark, Color.white, controlGraphics);
                    break;
                case Slider sl:
                    Transform bg = sl.transform.Find("Background");
                    if (bg != null) StyleImage(bg.GetComponent<Image>(), res.background, Color.white, controlGraphics);
                    if (sl.fillRect != null) StyleImage(sl.fillRect.GetComponent<Image>(), res.standard, Color.white, controlGraphics);
                    StyleImage(sl.targetGraphic as Image, res.knob, Color.white, controlGraphics);
                    break;
                case Scrollbar sb:
                    StyleImage(sb.GetComponent<Image>(), res.background, Color.white, controlGraphics);
                    StyleImage(sb.targetGraphic as Image, res.standard, Color.white, controlGraphics);
                    break;
                case TMP_InputField f:
                    StyleImage(f.targetGraphic as Image, res.inputField, Color.white, controlGraphics);
                    break;
                case InputField lf:
                    StyleImage(lf.targetGraphic as Image, res.inputField, Color.white, controlGraphics);
                    break;
                case TMP_Dropdown d:
                    StyleImage(d.targetGraphic as Image, res.standard, Color.white, controlGraphics);
                    Transform arrow = d.transform.Find("Arrow");
                    if (arrow != null) StyleImage(arrow.GetComponent<Image>(), res.dropdown, Color.white, controlGraphics);
                    break;
                default:
                    StyleImage(s.targetGraphic as Image, res.standard, Color.white, controlGraphics);
                    break;
            }
        }

        // Plain boxes (no art) → default Panel
        foreach (Image img in root.GetComponentsInChildren<Image>(true))
        {
            if (controlGraphics.Contains(img) || img.GetComponent<Mask>() != null) continue;
            if (img.sprite != null && !IsBuiltinSprite(img.sprite)) continue;   // real art / icon
            if (img.preserveAspect) continue;                                    // empty icon slot
            if (img.GetComponent<RectMask2D>() != null && img.color.a < 0.01f) continue;
            StyleImage(img, res.background, DefaultPanel, controlGraphics);
        }

        foreach (Shadow fx in root.GetComponentsInChildren<Shadow>(true))   // also Outline
            Undo.DestroyObjectImmediate(fx);

        TMP_FontAsset tmpFont = TMP_Settings.defaultFontAsset;
        foreach (TMP_Text t in root.GetComponentsInChildren<TMP_Text>(true))
        {
            Undo.RecordObject(t, "Default look");
            if (tmpFont != null) { t.font = tmpFont; t.fontSharedMaterial = tmpFont.material; }
            t.fontStyle = FontStyles.Normal;
            t.enableVertexGradient = false;
            t.color = OnLightSurface(t.transform, root.transform) ? DefaultTextDark : Color.white;
        }

        Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        foreach (Text t in root.GetComponentsInChildren<Text>(true))
        {
            Undo.RecordObject(t, "Default look");
            if (legacyFont != null) t.font = legacyFont;
            t.fontStyle = FontStyle.Normal;
            t.color = OnLightSurface(t.transform, root.transform) ? DefaultTextDark : Color.white;
        }
    }

    private static void StyleImage(Image img, Sprite sprite, Color color, HashSet<Graphic> done)
    {
        if (img == null) return;
        Undo.RecordObject(img, "Default look");
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = color;
        img.material = null;
        done.Add(img);
    }

    private static bool IsBuiltinSprite(Sprite s) =>
        AssetDatabase.GetAssetPath(s).StartsWith("Resources/unity_builtin_extra");

    /// <summary>True when the text sits on a visible default surface (button, panel) → dark text.</summary>
    private static bool OnLightSurface(Transform t, Transform stopAt)
    {
        for (Transform p = t.parent; p != null; p = p.parent)
        {
            Image img = p.GetComponent<Image>();
            if (img != null && img.enabled && img.color.a > 0.2f) return true;
            if (p == stopAt) break;
        }
        return false;
    }

    // ================================================================
    //  ADD MISSING SCENE OBJECTS
    // ================================================================

    /// <summary>
    /// Puts things into the open scene(s) and prefabs as REAL objects (no
    /// runtime-created UI): Quit to Menu button, Auto button, UIScaleManager,
    /// achievement popup placement, AchievementTracker on player prefabs,
    /// Input System UI module on EventSystems. Safe to re-run.
    /// </summary>
    [MenuItem("Tools/Go To Hell/UI/Add Missing Scene Objects (open scenes)")]
    public static void AddMissingSceneObjects()
    {
        var log = new List<string>();

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            GameObject[] roots = scene.GetRootGameObjects();

            foreach (PauseMenu p in roots.SelectMany(r => r.GetComponentsInChildren<PauseMenu>(true)))
            {
                if (p.quitToMenuButton != null || p.restartButton == null) continue;
                p.quitToMenuButton = DuplicateButton(p.restartButton, "QuitToMenuButton", "Quit to Menu");
                EditorUtility.SetDirty(p);
                log.Add("Quit to Menu button");
            }

            foreach (CharacterCreateUI c in roots.SelectMany(r => r.GetComponentsInChildren<CharacterCreateUI>(true)))
            {
                if (c.autoButton == null && c.createButton != null)
                {
                    c.autoButton = DuplicateButton(c.createButton, "AutoButton", "Auto");
                    EditorUtility.SetDirty(c);
                    log.Add("Auto button (next to Create — move it where you like)");
                }
                if (c.statRows == null || c.statRows.Length == 0)
                    Debug.LogWarning("[GoToHellSetup] CharacterCreateUI has no stat rows — delete MenuFlowCanvas and run " +
                                     "Characters → Build Main Menu Flow UI to rebuild it with scene rows.");
            }

            foreach (AchievementPopupUI a in roots.SelectMany(r => r.GetComponentsInChildren<AchievementPopupUI>(true)))
            {
                if (a.panel == null) continue;
                Undo.RecordObject(a.panel, "Popup placement");
                a.panel.anchorMin = a.panel.anchorMax = a.panel.pivot = new Vector2(1f, 0f);
                a.panel.anchoredPosition = new Vector2(-40f, 40f);
                Canvas c = a.panel.GetComponent<Canvas>();
                if (c == null) c = Undo.AddComponent<Canvas>(a.panel.gameObject);
                c.overrideSorting = true;
                c.sortingOrder = short.MaxValue;
                Undo.RecordObject(a, "Popup placement");
                a.shownX = -40f;
                a.hiddenX = a.panel.rect.width + 40f;
                EditorUtility.SetDirty(a);
                log.Add("Achievement popup → bottom-right, drawn above all UI");
            }

            if (!roots.Any(r => r.GetComponentInChildren<UIScaleManager>(true) != null))
            {
                var go = new GameObject("UIScaleManager", typeof(UIScaleManager));
                SceneManager.MoveGameObjectToScene(go, scene);
                Undo.RegisterCreatedObjectUndo(go, "UIScaleManager");
                log.Add($"UIScaleManager object ({scene.name})");
            }

            foreach (UnityEngine.EventSystems.EventSystem es in roots.SelectMany(r => r.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true)))
                if (InputModuleBridge.Fix(es.gameObject)) log.Add("EventSystem → Input System UI module");

            EditorSceneManager.MarkSceneDirty(scene);
        }

        foreach (GameObject prefab in LoadAllAssets<ClassDefinition>().Where(c => c.playerPrefab != null)
                                                                       .Select(c => c.playerPrefab).Distinct())
        {
            if (prefab.GetComponent<AchievementTracker>() != null) continue;
            using (var edit = new PrefabUtility.EditPrefabContentsScope(AssetDatabase.GetAssetPath(prefab)))
                edit.prefabContentsRoot.AddComponent<AchievementTracker>();
            log.Add($"AchievementTracker on {prefab.name}");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[GoToHellSetup] Added: " + (log.Count > 0 ? string.Join(", ", log) : "nothing — all present") +
                  ". Save the scene(s); open other scenes and run again.");
    }

    /// <summary>Scene copy of a button, placed right after it (below it if there's no layout group).</summary>
    private static Button DuplicateButton(Button source, string name, string label)
    {
        GameObject go = Object.Instantiate(source.gameObject, source.transform.parent);
        Undo.RegisterCreatedObjectUndo(go, name);
        go.name = name;
        go.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);
        Button b = go.GetComponent<Button>();
        b.onClick = new Button.ButtonClickedEvent();
        if (go.transform.parent.GetComponent<LayoutGroup>() == null)
        {
            RectTransform src = (RectTransform)source.transform;
            ((RectTransform)go.transform).anchoredPosition = src.anchoredPosition - new Vector2(0f, src.rect.height + 16f);
        }
        TMP_Text t = go.GetComponentInChildren<TMP_Text>(true);
        if (t != null) t.text = label;
        Text lt = go.GetComponentInChildren<Text>(true);
        if (lt != null) lt.text = label;
        return b;
    }

    // ================================================================
    //  INPUT ACTIONS ASSET
    // ================================================================

    [MenuItem("Tools/Go To Hell/Input/Create Input Actions Asset")]
    public static void CreateInputActionsAsset()
    {
#if ENABLE_INPUT_SYSTEM
        string path = $"Assets/Resources/{GameInput.ActionsResource}.inputactions";
        if (System.IO.File.Exists(path))
        {
            Debug.LogWarning($"[GoToHellSetup] {path} already exists — edit it in the Input Actions editor (double-click).");
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(path);
            return;
        }
        EnsureFolder("Assets/Resources");
        UnityEngine.InputSystem.InputActionAsset asset = GameInput.CreateDefaultActions();
        System.IO.File.WriteAllText(path, asset.ToJson());
        Object.DestroyImmediate(asset);
        AssetDatabase.ImportAsset(path);
        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(path);
        Debug.Log($"[GoToHellSetup] Created {path} — the game now reads its key bindings from it.");
#else
        Debug.LogWarning("[GoToHellSetup] Input System isn't active.");
#endif
    }

    // ================================================================
    //  SETTINGS PANEL
    // ================================================================

    [MenuItem("Tools/Go To Hell/UI/Build Settings Panel (open scene)")]
    public static void BuildSettingsPanel()
    {
        MainMenu menu = Object.FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include);
        PauseMenu pause = Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
        if (menu == null && pause == null)
        {
            Debug.LogWarning("[GoToHellSetup] Open a scene with MainMenu or PauseMenu first.");
            return;
        }
        if (Object.FindFirstObjectByType<SettingsUI>(FindObjectsInactive.Include) != null)
        {
            Debug.LogWarning("[GoToHellSetup] This scene already has a SettingsUI — delete it first to rebuild.");
            return;
        }

        // Gameplay: live inside the pause menu's own canvas so no other canvas
        // (HUD, sort 1000+) can sit on top and swallow clicks. Menu: own canvas.
        Canvas pauseCanvas = PauseCanvas(pause);
        GameObject host = pauseCanvas != null ? SettingsHost(pauseCanvas) : CreateCanvas("SettingsCanvas", 150).gameObject;
        DefaultControls.Resources res = UiResources();
        TMP_DefaultControls.Resources tmp = TmpResources();

        GameObject panel = DefaultControls.CreatePanel(res);
        panel.name = "SettingsPanel";
        panel.transform.SetParent(host.transform, false);
        RectTransform prt = (RectTransform)panel.transform;
        prt.anchorMin = new Vector2(0.15f, 0.1f);
        prt.anchorMax = new Vector2(0.85f, 0.9f);
        prt.offsetMin = prt.offsetMax = Vector2.zero;

        SettingsUI ui = host.AddComponent<SettingsUI>();
        ui.panel = panel;

        TMP_Text title = Tmp(TMP_DefaultControls.CreateText(tmp), panel.transform, "Title", "Settings");
        title.fontSize = 36;
        title.alignment = TextAlignmentOptions.Center;
        Place(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -40), new Vector2(0, 60));

        // Tabs row
        string[] tabNames = { "Graphics", "Gameplay", "Sound", "Controls" };
        var tabs = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            GameObject b = TMP_DefaultControls.CreateButton(tmp);
            b.name = tabNames[i] + "Tab";
            b.transform.SetParent(panel.transform, false);
            b.GetComponentInChildren<TMP_Text>().text = tabNames[i];
            float x0 = 0.05f + i * 0.225f;
            Place((RectTransform)b.transform, new Vector2(x0, 1), new Vector2(x0 + 0.21f, 1), new Vector2(0, -105), new Vector2(0, 40));
            tabs[i] = b.GetComponent<Button>();
        }
        ui.graphicsTab = tabs[0]; ui.gameplayTab = tabs[1]; ui.soundTab = tabs[2]; ui.controlsTab = tabs[3];

        // One page per tab — all controls are real scene objects
        GameObject PageObj(string name)
        {
            GameObject page = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            page.transform.SetParent(panel.transform, false);
            RectTransform rt = (RectTransform)page.transform;
            rt.anchorMin = new Vector2(0.05f, 0.14f);
            rt.anchorMax = new Vector2(0.95f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0, -140);
            VerticalLayoutGroup v = page.GetComponent<VerticalLayoutGroup>();
            v.spacing = 8; v.childAlignment = TextAnchor.UpperLeft;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return page;
        }

        ui.graphicsPage = PageObj("GraphicsPage");
        ui.gameplayPage = PageObj("GameplayPage");
        ui.soundPage = PageObj("SoundPage");
        ui.controlsPage = PageObj("ControlsPage");

        Transform g = ui.graphicsPage.transform;
        ui.resolutionDropdown = SDropdown(SRow(tmp, g, "Resolution"), tmp, "1920 x 1080");
        ui.windowModeDropdown = SDropdown(SRow(tmp, g, "Window Mode"), tmp, "Fullscreen", "Borderless", "Windowed");
        ui.vsyncToggle = SToggle(SRow(tmp, g, "VSync"), res);
        RectTransform fpsRow = SRow(tmp, g, "Max FPS");
        ui.maxFpsSlider = SSlider(fpsRow, res, 3, 31, true);
        ui.maxFpsValue = SValue(fpsRow, tmp);
        ui.antiAliasingDropdown = SDropdown(SRow(tmp, g, "Anti-Aliasing"), tmp, "Off", "2x MSAA", "4x MSAA", "8x MSAA");
        RectTransform uiRow = SRow(tmp, g, "UI Scale");
        ui.uiScaleSlider = SSlider(uiRow, res, UIScaleManager.MinUserScale, UIScaleManager.MaxUserScale, false);
        ui.uiScaleValue = SValue(uiRow, tmp);

        Transform gp = ui.gameplayPage.transform;
        ui.cameraShakeToggle = SToggle(SRow(tmp, gp, "Camera Shake"), res);
        RectTransform shakeRow = SRow(tmp, gp, "Shake Intensity");
        ui.shakeIntensitySlider = SSlider(shakeRow, res, 0f, 5f, false);
        ui.shakeIntensityValue = SValue(shakeRow, tmp);
        ui.hitStopToggle = SToggle(SRow(tmp, gp, "Hit Stop"), res);

        Transform sp = ui.soundPage.transform;
        RectTransform mRow = SRow(tmp, sp, "Master Volume");
        ui.masterVolumeSlider = SSlider(mRow, res, 0f, 1f, false); ui.masterVolumeValue = SValue(mRow, tmp);
        RectTransform muRow = SRow(tmp, sp, "Music Volume");
        ui.musicVolumeSlider = SSlider(muRow, res, 0f, 1f, false); ui.musicVolumeValue = SValue(muRow, tmp);
        RectTransform fxRow = SRow(tmp, sp, "Effects Volume");
        ui.sfxVolumeSlider = SSlider(fxRow, res, 0f, 1f, false); ui.sfxVolumeValue = SValue(fxRow, tmp);

        Transform cp = ui.controlsPage.transform;
        RectTransform head = SRow(tmp, cp, "Action");
        SValue(head, tmp).text = "Keyboard / Mouse";
        SValue(head, tmp).text = "Gamepad";
        var rows = new List<SettingsUI.BindingRow>();
        foreach (GameAction a in new[] { GameAction.Jump, GameAction.Attack, GameAction.AltAttack, GameAction.Skill,
                                         GameAction.Interact, GameAction.LevelUp, GameAction.Pause, GameAction.ToggleStats })
        {
            RectTransform row = SRow(tmp, cp, ObjectNames.NicifyVariableName(a.ToString()));
            rows.Add(new SettingsUI.BindingRow { action = a, keyboardButton = SButton(row, tmp, "key"), gamepadButton = SButton(row, tmp, "button") });
        }
        ui.bindingRows = rows.ToArray();

        // Bottom buttons
        ui.resetButton = BottomButton(tmp, panel.transform, "Reset", "Reset to Defaults", 0.05f, 0.30f);
        ui.backButton = BottomButton(tmp, panel.transform, "Back", "Back", 0.70f, 0.95f);

        ui.gameplayPage.SetActive(false);
        ui.soundPage.SetActive(false);
        ui.controlsPage.SetActive(false);
        panel.SetActive(false);

        // Wire into the menu that owns settings; hide its old panel
        if (menu != null)
        {
            Undo.RecordObject(menu, "Wire settings");
            if (menu.settingsPanel != null) RetireOldPanel(menu.settingsPanel);
            menu.settingsPanel = panel;
            menu.settingsBackButton = ui.backButton;
            EditorUtility.SetDirty(menu);
        }
        if (pause != null)
        {
            Undo.RecordObject(pause, "Wire settings");
            if (pause.settingsPanel != null) RetireOldPanel(pause.settingsPanel);
            pause.settingsPanel = panel;
            pause.settingsBackButton = ui.backButton;
            EditorUtility.SetDirty(pause);
        }

        FinishScene(host, "Settings panel built (Graphics / Gameplay / Sound / Controls) and wired.");
    }

    private static Canvas PauseCanvas(PauseMenu pause)
    {
        if (pause == null) return null;
        GameObject anchor = pause.pausePanel != null ? pause.pausePanel : pause.gameObject;
        Canvas c = anchor.GetComponentInParent<Canvas>(true);
        return c != null ? c.rootCanvas : null;
    }

    /// <summary>Full-screen "Settings" object as the LAST child of the canvas (drawn and clicked on top).</summary>
    private static GameObject SettingsHost(Canvas canvas)
    {
        GameObject host = new GameObject("Settings", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(host, "Settings host");
        host.transform.SetParent(canvas.transform, false);
        RectTransform rt = (RectTransform)host.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        host.transform.SetAsLastSibling();
        return host;
    }

    [MenuItem("Tools/Go To Hell/UI/Move Settings Into Pause Canvas (open scene)")]
    public static void MoveSettingsIntoPauseCanvas()
    {
        PauseMenu pause = Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
        SettingsUI old = Object.FindFirstObjectByType<SettingsUI>(FindObjectsInactive.Include);
        Canvas pauseCanvas = PauseCanvas(pause);
        if (pauseCanvas == null || old == null)
        {
            Debug.LogWarning("[GoToHellSetup] Needs a scene with PauseMenu and a SettingsUI.");
            return;
        }
        if (old.transform.IsChildOf(pauseCanvas.transform))
        {
            old.transform.SetAsLastSibling();
            Debug.Log("[GoToHellSetup] Settings already inside the pause canvas — moved to last sibling.");
            return;
        }

        GameObject host = SettingsHost(pauseCanvas);
        SettingsUI ui = host.AddComponent<SettingsUI>();
        EditorUtility.CopySerialized(old, ui);   // keeps every control reference
        GameObject oldRoot = old.gameObject;
        for (int i = oldRoot.transform.childCount - 1; i >= 0; i--)
            Undo.SetTransformParent(oldRoot.transform.GetChild(i), host.transform, "Move settings");
        foreach (Transform child in host.transform) ((RectTransform)child).localScale = Vector3.one;
        Undo.DestroyObjectImmediate(oldRoot);

        FinishScene(host, "Settings moved into the pause canvas (" + pauseCanvas.name + ").");
    }

    private static RectTransform SRow(TMP_DefaultControls.Resources tmp, Transform page, string label)
    {
        GameObject row = new GameObject(label, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(page, false);
        HorizontalLayoutGroup h = row.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 10; h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = true; h.childForceExpandHeight = false;
        row.GetComponent<LayoutElement>().minHeight = 34;
        TMP_Text t = Tmp(TMP_DefaultControls.CreateText(tmp), row.transform, "Label", label);
        t.fontSize = 20;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        return (RectTransform)row.transform;
    }

    private static T SFit<T>(GameObject go, Transform row) where T : Component
    {
        go.transform.SetParent(row, false);
        go.AddComponent<LayoutElement>().minHeight = 30;
        return go.GetComponent<T>();
    }

    private static Toggle SToggle(Transform row, DefaultControls.Resources res)
    {
        GameObject go = DefaultControls.CreateToggle(res);
        go.name = "Toggle";
        Transform l = go.transform.Find("Label");
        if (l != null) Object.DestroyImmediate(l.gameObject);
        return SFit<Toggle>(go, row);
    }

    private static Slider SSlider(Transform row, DefaultControls.Resources res, float min, float max, bool whole)
    {
        GameObject go = DefaultControls.CreateSlider(res);
        go.name = "Slider";
        Slider s = SFit<Slider>(go, row);
        s.minValue = min; s.maxValue = max; s.wholeNumbers = whole;
        return s;
    }

    private static TMP_Text SValue(Transform row, TMP_DefaultControls.Resources tmp)
    {
        TMP_Text t = Tmp(TMP_DefaultControls.CreateText(tmp), row, "Value", "");
        t.fontSize = 20;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        return t;
    }

    private static TMP_Dropdown SDropdown(Transform row, TMP_DefaultControls.Resources tmp, params string[] options)
    {
        GameObject go = TMP_DefaultControls.CreateDropdown(tmp);
        go.name = "Dropdown";
        TMP_Dropdown d = SFit<TMP_Dropdown>(go, row);
        d.ClearOptions();
        d.AddOptions(options.ToList());
        return d;
    }

    private static Button SButton(Transform row, TMP_DefaultControls.Resources tmp, string label)
    {
        GameObject go = TMP_DefaultControls.CreateButton(tmp);
        go.name = "Rebind";
        go.GetComponentInChildren<TMP_Text>().text = label;
        return SFit<Button>(go, row);
    }

    private static void RetireOldPanel(GameObject old)
    {
        old.SetActive(false);
        if (!old.name.StartsWith("(old) ")) old.name = "(old) " + old.name;
        Debug.Log($"[GoToHellSetup] Old settings panel '{old.name}' hidden — delete it when you're happy.");
    }

    private static TMP_Text Tmp(GameObject go, Transform parent, string name, string text)
    {
        go.name = name;
        go.transform.SetParent(parent, false);
        TMP_Text t = go.GetComponent<TMP_Text>();
        t.text = text;
        return t;
    }

    private static Button BottomButton(TMP_DefaultControls.Resources tmp, Transform parent, string name, string label,
                                       float x0, float x1)
    {
        GameObject b = TMP_DefaultControls.CreateButton(tmp);
        b.name = name;
        b.transform.SetParent(parent, false);
        b.GetComponentInChildren<TMP_Text>().text = label;
        RectTransform rt = (RectTransform)b.transform;
        rt.anchorMin = new Vector2(x0, 0.03f);
        rt.anchorMax = new Vector2(x1, 0.1f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return b.GetComponent<Button>();
    }

    private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}
