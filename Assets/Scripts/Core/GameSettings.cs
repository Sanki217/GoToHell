using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Every player setting in one place: values, defaults, saving (PlayerPrefs)
/// and applying them to the engine. UI (SettingsUI) only reads/writes these
/// properties; gameplay reads them (CameraFollow → shake, HitStopManager →
/// hit-stop, audio → volumes).
///
/// Applied once before the first scene loads, then again on every change.
/// Adding a setting: one property here + one row in SettingsUI.
/// </summary>
public static class GameSettings
{
    public enum WindowMode { Fullscreen, Borderless, Windowed }

    /// <summary>Fired after any setting changes (live listeners refresh).</summary>
    public static event Action OnChanged;

    /// <summary>Max-FPS slider value meaning "no limit".</summary>
    public const int UnlimitedFps = 0;
    public static readonly int[] AntiAliasingLevels = { 0, 2, 4, 8 };

    // ================================================================
    //  GRAPHICS
    // ================================================================

    public static int ResolutionWidth
    {
        get => PlayerPrefs.GetInt("set.resW", Screen.currentResolution.width);
        private set => PlayerPrefs.SetInt("set.resW", value);
    }

    public static int ResolutionHeight
    {
        get => PlayerPrefs.GetInt("set.resH", Screen.currentResolution.height);
        private set => PlayerPrefs.SetInt("set.resH", value);
    }

    public static void SetResolution(int width, int height)
    {
        ResolutionWidth = width;
        ResolutionHeight = height;
        Changed(applyDisplay: true);
    }

    public static WindowMode Window
    {
        get => (WindowMode)PlayerPrefs.GetInt("set.window", (int)WindowMode.Borderless);
        set { PlayerPrefs.SetInt("set.window", (int)value); Changed(applyDisplay: true); }
    }

    public static bool VSync
    {
        get => PlayerPrefs.GetInt(GameSettingsBootstrap.VSyncPref, 0) == 1;
        set { PlayerPrefs.SetInt(GameSettingsBootstrap.VSyncPref, value ? 1 : 0); Changed(); }
    }

    /// <summary>Frame cap; UnlimitedFps (0) = uncapped. Ignored while VSync is on.</summary>
    public static int MaxFps
    {
        get => PlayerPrefs.GetInt("set.maxFps", UnlimitedFps);
        set { PlayerPrefs.SetInt("set.maxFps", Mathf.Max(0, value)); Changed(); }
    }

    /// <summary>MSAA samples: 0, 2, 4 or 8.</summary>
    public static int AntiAliasing
    {
        get => PlayerPrefs.GetInt("set.msaa", 2);
        set { PlayerPrefs.SetInt("set.msaa", value); Changed(); }
    }

    /// <summary>UI size (0.75–1.5), applied by UIScaleManager.</summary>
    public static float UiScale
    {
        get => UIScaleManager.UserScale;
        set { UIScaleManager.UserScale = value; Changed(); }
    }

    // ================================================================
    //  GAMEPLAY
    // ================================================================

    public static bool CameraShake
    {
        get => PlayerPrefs.GetInt("set.shake", 1) == 1;
        set { PlayerPrefs.SetInt("set.shake", value ? 1 : 0); Changed(); }
    }

    /// <summary>Shake strength multiplier, 0–5 (0–500%).</summary>
    public static float ShakeIntensity
    {
        get => PlayerPrefs.GetFloat("set.shakeAmt", 1f);
        set { PlayerPrefs.SetFloat("set.shakeAmt", Mathf.Clamp(value, 0f, 5f)); Changed(); }
    }

    public static bool HitStop
    {
        get => PlayerPrefs.GetInt("set.hitstop", 1) == 1;
        set { PlayerPrefs.SetInt("set.hitstop", value ? 1 : 0); Changed(); }
    }

    // ================================================================
    //  SOUND (0–1)
    // ================================================================

    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat("set.volMaster", 1f);
        set { PlayerPrefs.SetFloat("set.volMaster", Mathf.Clamp01(value)); Changed(); }
    }

    /// <summary>For music sources (multiply into AudioSource.volume when music is added).</summary>
    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat("set.volMusic", 0.8f);
        set { PlayerPrefs.SetFloat("set.volMusic", Mathf.Clamp01(value)); Changed(); }
    }

    /// <summary>For sound effects (multiply into AudioSource.volume / PlayOneShot scale).</summary>
    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat("set.volSfx", 1f);
        set { PlayerPrefs.SetFloat("set.volSfx", Mathf.Clamp01(value)); Changed(); }
    }

    // ================================================================
    //  APPLY
    // ================================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics() => OnChanged = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyAtBoot()
    {
        ApplyQuality();
#if !UNITY_EDITOR
        ApplyDisplay();   // never resize the editor's Game view
#endif
    }

    /// <summary>Distinct resolutions the display supports, largest last.</summary>
    public static List<Vector2Int> AvailableResolutions()
    {
        var list = new List<Vector2Int>();
        foreach (Resolution r in Screen.resolutions)
        {
            var v = new Vector2Int(r.width, r.height);
            if (!list.Contains(v)) list.Add(v);
        }
        if (list.Count == 0) list.Add(new Vector2Int(Screen.width, Screen.height));
        list.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        return list;
    }

    public static void ResetToDefaults()
    {
        foreach (string k in new[] { "set.resW", "set.resH", "set.window", "set.maxFps", "set.msaa", "set.shake",
                                     "set.shakeAmt", "set.hitstop", "set.volMaster", "set.volMusic", "set.volSfx",
                                     GameSettingsBootstrap.VSyncPref })
            PlayerPrefs.DeleteKey(k);
        UIScaleManager.UserScale = 1f;
        Changed(applyDisplay: true);
    }

    private static void Changed(bool applyDisplay = false)
    {
        PlayerPrefs.Save();
        ApplyQuality();
        if (applyDisplay) ApplyDisplay();
        OnChanged?.Invoke();
    }

    private static void ApplyQuality()
    {
        QualitySettings.vSyncCount = VSync ? 1 : 0;
        Application.targetFrameRate = VSync || MaxFps == UnlimitedFps ? -1 : MaxFps;
        QualitySettings.antiAliasing = AntiAliasing;
        AudioListener.volume = MasterVolume;
    }

    private static void ApplyDisplay()
    {
        FullScreenMode mode = Window switch
        {
            WindowMode.Fullscreen => FullScreenMode.ExclusiveFullScreen,
            WindowMode.Windowed   => FullScreenMode.Windowed,
            _                     => FullScreenMode.FullScreenWindow,
        };
        Screen.SetResolution(ResolutionWidth, ResolutionHeight, mode);
    }
}
