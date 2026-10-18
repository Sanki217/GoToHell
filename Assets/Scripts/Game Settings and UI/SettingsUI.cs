using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tabbed settings screen (Graphics / Gameplay / Sound / Controls) used by
/// both the main menu and the pause menu. Every control is a normal object
/// in the scene, assigned below — move, restyle or delete them freely; an
/// unassigned control is simply skipped. Values live in GameSettings.
///
/// Built by Tools → Go To Hell → UI → Build Settings Panel, which also wires
/// MainMenu / PauseMenu.
/// </summary>
public class SettingsUI : MonoBehaviour
{
    [Serializable]
    public class BindingRow
    {
        public GameAction action;
        public Button keyboardButton;
        public Button gamepadButton;
    }

    [Header("Structure")]
    public GameObject panel;
    public Button resetButton;
    [Tooltip("Back/close button — MainMenu / PauseMenu listen to it.")]
    public Button backButton;

    [Header("Tabs (button + the page it shows)")]
    public Button graphicsTab;   public GameObject graphicsPage;
    public Button gameplayTab;   public GameObject gameplayPage;
    public Button soundTab;      public GameObject soundPage;
    public Button controlsTab;   public GameObject controlsPage;

    [Header("Graphics")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown windowModeDropdown;    // Fullscreen, Borderless, Windowed
    public Toggle vsyncToggle;
    [Tooltip("Whole numbers 3–31: ×10 FPS, 31 = Unlimited.")]
    public Slider maxFpsSlider;
    public TMP_Text maxFpsValue;
    public TMP_Dropdown antiAliasingDropdown;  // Off, 2x, 4x, 8x
    public Slider uiScaleSlider;
    public TMP_Text uiScaleValue;

    [Header("Gameplay")]
    public Toggle cameraShakeToggle;
    [Tooltip("0–5 (0–500%).")]
    public Slider shakeIntensitySlider;
    public TMP_Text shakeIntensityValue;
    public Toggle hitStopToggle;

    [Header("Sound")]
    public Slider masterVolumeSlider;  public TMP_Text masterVolumeValue;
    public Slider musicVolumeSlider;   public TMP_Text musicVolumeValue;
    public Slider sfxVolumeSlider;     public TMP_Text sfxVolumeValue;

    [Header("Controls")]
    public BindingRow[] bindingRows = new BindingRow[0];

    private GameObject currentPage;
    private List<Vector2Int> resolutions = new List<Vector2Int>();

    // ================================================================
    //  SETUP — listeners once
    // ================================================================

    private void Awake()
    {
        Tab(graphicsTab, graphicsPage);
        Tab(gameplayTab, gameplayPage);
        Tab(soundTab, soundPage);
        Tab(controlsTab, controlsPage);

        if (resetButton != null) resetButton.onClick.AddListener(ResetCurrent);

        On(resolutionDropdown, i => { if (i < resolutions.Count) GameSettings.SetResolution(resolutions[i].x, resolutions[i].y); });
        On(windowModeDropdown, i => GameSettings.Window = (GameSettings.WindowMode)i);
        On(vsyncToggle, v => GameSettings.VSync = v);
        On(maxFpsSlider, v => { GameSettings.MaxFps = v >= 31 ? GameSettings.UnlimitedFps : Mathf.RoundToInt(v) * 10; Show(maxFpsValue, FpsText(v)); });
        On(antiAliasingDropdown, i => GameSettings.AntiAliasing = GameSettings.AntiAliasingLevels[Mathf.Clamp(i, 0, 3)]);
        On(uiScaleSlider, v => { GameSettings.UiScale = v; Show(uiScaleValue, Percent(v)); });

        On(cameraShakeToggle, v => GameSettings.CameraShake = v);
        On(shakeIntensitySlider, v => { GameSettings.ShakeIntensity = v; Show(shakeIntensityValue, Percent(v)); });
        On(hitStopToggle, v => GameSettings.HitStop = v);

        On(masterVolumeSlider, v => { GameSettings.MasterVolume = v; Show(masterVolumeValue, Percent(v)); });
        On(musicVolumeSlider, v => { GameSettings.MusicVolume = v; Show(musicVolumeValue, Percent(v)); });
        On(sfxVolumeSlider, v => { GameSettings.SfxVolume = v; Show(sfxVolumeValue, Percent(v)); });

        foreach (BindingRow row in bindingRows)
        {
            if (row == null) continue;
            Rebind(row.keyboardButton, row.action, InputScheme.KeyboardMouse);
            Rebind(row.gamepadButton, row.action, InputScheme.Gamepad);
        }
    }

    private void OnEnable()
    {
        Refresh();
        if (currentPage == null) Open(graphicsPage);
    }

    private void Tab(Button tab, GameObject page)
    {
        if (tab != null) tab.onClick.AddListener(() => Open(page));
    }

    private void Open(GameObject page)
    {
        currentPage = page;
        foreach (GameObject p in new[] { graphicsPage, gameplayPage, soundPage, controlsPage })
            if (p != null) p.SetActive(p == page);
        Interactable(graphicsTab, page != graphicsPage);
        Interactable(gameplayTab, page != gameplayPage);
        Interactable(soundTab, page != soundPage);
        Interactable(controlsTab, page != controlsPage);
    }

    private void ResetCurrent()
    {
        if (currentPage != null && currentPage == controlsPage) GameInput.ResetBindings();
        else GameSettings.ResetToDefaults();
        Refresh();
    }

    // ================================================================
    //  REFRESH — current values into the controls (no callbacks)
    // ================================================================

    private void Refresh()
    {
        if (resolutionDropdown != null)
        {
            resolutions = GameSettings.AvailableResolutions();
            var names = new List<string>();
            int index = resolutions.Count - 1;
            for (int i = 0; i < resolutions.Count; i++)
            {
                names.Add($"{resolutions[i].x} x {resolutions[i].y}");
                if (resolutions[i].x == GameSettings.ResolutionWidth && resolutions[i].y == GameSettings.ResolutionHeight) index = i;
            }
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(names);
            resolutionDropdown.SetValueWithoutNotify(index);
        }

        if (windowModeDropdown != null) windowModeDropdown.SetValueWithoutNotify((int)GameSettings.Window);
        if (vsyncToggle != null) vsyncToggle.SetIsOnWithoutNotify(GameSettings.VSync);
        if (maxFpsSlider != null)
        {
            float v = GameSettings.MaxFps == GameSettings.UnlimitedFps ? 31 : Mathf.Clamp(GameSettings.MaxFps / 10, 3, 30);
            maxFpsSlider.SetValueWithoutNotify(v);
            Show(maxFpsValue, FpsText(v));
        }
        if (antiAliasingDropdown != null)
            antiAliasingDropdown.SetValueWithoutNotify(Mathf.Max(0, Array.IndexOf(GameSettings.AntiAliasingLevels, GameSettings.AntiAliasing)));
        SetSlider(uiScaleSlider, uiScaleValue, GameSettings.UiScale);

        if (cameraShakeToggle != null) cameraShakeToggle.SetIsOnWithoutNotify(GameSettings.CameraShake);
        SetSlider(shakeIntensitySlider, shakeIntensityValue, GameSettings.ShakeIntensity);
        if (hitStopToggle != null) hitStopToggle.SetIsOnWithoutNotify(GameSettings.HitStop);

        SetSlider(masterVolumeSlider, masterVolumeValue, GameSettings.MasterVolume);
        SetSlider(musicVolumeSlider, musicVolumeValue, GameSettings.MusicVolume);
        SetSlider(sfxVolumeSlider, sfxVolumeValue, GameSettings.SfxVolume);

        foreach (BindingRow row in bindingRows)
        {
            if (row == null) continue;
            SetLabel(row.keyboardButton, GameInput.BindingDisplay(row.action, InputScheme.KeyboardMouse));
            SetLabel(row.gamepadButton, GameInput.BindingDisplay(row.action, InputScheme.Gamepad));
        }
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    private void Rebind(Button b, GameAction action, InputScheme scheme)
    {
        if (b == null) return;
        b.onClick.AddListener(() =>
        {
            SetLabel(b, "Press a key… (Esc cancels)");
            bool started = GameInput.StartRebind(action, scheme, _ => SetLabel(b, GameInput.BindingDisplay(action, scheme)));
            if (!started) SetLabel(b, GameInput.BindingDisplay(action, scheme));
        });
    }

    private static void On(Toggle t, Action<bool> a)        { if (t != null) t.onValueChanged.AddListener(v => a(v)); }
    private static void On(Slider s, Action<float> a)       { if (s != null) s.onValueChanged.AddListener(v => a(v)); }
    private static void On(TMP_Dropdown d, Action<int> a)   { if (d != null) d.onValueChanged.AddListener(v => a(v)); }

    private static void SetSlider(Slider s, TMP_Text label, float value)
    {
        if (s == null) return;
        s.SetValueWithoutNotify(value);
        Show(label, Percent(value));
    }

    private static void Show(TMP_Text t, string text) { if (t != null) t.text = text; }

    private static void SetLabel(Button b, string text)
    {
        TMP_Text t = b != null ? b.GetComponentInChildren<TMP_Text>(true) : null;
        if (t != null) t.text = text;
    }

    private static void Interactable(Button b, bool on) { if (b != null) b.interactable = on; }

    private static string Percent(float v) => $"{v * 100f:0}%";
    private static string FpsText(float v) => v >= 31 ? "Unlimited" : $"{Mathf.RoundToInt(v) * 10}";
}
