using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Makes every screen-space UI scale correctly on any monitor — 720p laptop,
/// 1440p, 4K, 21:9 ultrawide, 16:10, phones and tablets — with no per-scene
/// setup and a player-facing "UI Scale" setting.
///
/// RULES IT ENFORCES on every root Canvas that isn't world-space:
///   • CanvasScaler = Scale With Screen Size, reference 1920×1080
///     (author all UI at 1080p; it scales up/down from there).
///   • Match = height on screens 16:9 or wider (ultrawide gets more side
///     room, never giant UI), width on narrower screens (4:3, 16:10, phones)
///     so nothing is pushed off the sides.
///   • × the player's UI Scale (Settings, 75–150%) × a readability boost on
///     touch devices.
///
/// SETUP: one "UIScaleManager" object per scene (Tools → Go To Hell → UI →
/// Add Missing Scene Objects places it). Canvases spawned later (chest UI,
/// popups) are picked up within a second.
/// Opt a canvas out or give it its own reference with a ResponsiveCanvas
/// component. Anchor UI to the edges/corners it belongs to — scaling fixes
/// SIZE; anchors fix POSITION.
/// </summary>
public class UIScaleManager : MonoBehaviour
{
    public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    public const float MinUserScale = 0.75f;
    public const float MaxUserScale = 1.5f;
    private const string ScalePrefKey = "ui.scale";

    /// <summary>Extra scale on touch-first devices (fingers + small screens).</summary>
    public static float TouchScaleBoost = 1.15f;

    private static UIScaleManager instance;
    private static float userScale = -1f;

    private readonly List<CanvasScaler> scratch = new List<CanvasScaler>();
    private int lastWidth, lastHeight;
    private float nextScan;

    /// <summary>Player UI scale (1 = default). Persisted; applies immediately.</summary>
    public static float UserScale
    {
        get
        {
            if (userScale < 0f) userScale = Mathf.Clamp(PlayerPrefs.GetFloat(ScalePrefKey, 1f), MinUserScale, MaxUserScale);
            return userScale;
        }
        set
        {
            userScale = Mathf.Clamp(value, MinUserScale, MaxUserScale);
            PlayerPrefs.SetFloat(ScalePrefKey, userScale);
            PlayerPrefs.Save();
            if (instance != null) instance.ApplyAll();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics()
    {
        instance = null;
        userScale = -1f;
    }

    private void OnEnable()
    {
        instance = this;
        ApplyAll();
    }

    private void OnDisable()
    {
        if (instance == this) instance = null;
    }

    private void Update()
    {
        bool resized = Screen.width != lastWidth || Screen.height != lastHeight;
        if (!resized && Time.unscaledTime < nextScan) return;
        ApplyAll();
    }

    /// <summary>Re-applies scaling to every canvas now.</summary>
    public void ApplyAll()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        nextScan = Time.unscaledTime + 1f;

        scratch.Clear();
        scratch.AddRange(FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        foreach (CanvasScaler s in scratch) Apply(s);
        scratch.Clear();
    }

    /// <summary>Applies the scaling rules to one scaler (no-op for world-space / nested / exempt canvases).</summary>
    public static void Apply(CanvasScaler scaler)
    {
        if (scaler == null || !scaler.TryGetComponent(out Canvas canvas)) return;
        if (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace) return;

        Vector2 reference = ReferenceResolution;
        float matchOverride = -1f;
        if (scaler.TryGetComponent(out ResponsiveCanvas rc))
        {
            if (rc.exempt) return;
            if (rc.referenceResolution.x > 0f && rc.referenceResolution.y > 0f) reference = rc.referenceResolution;
            matchOverride = rc.matchOverride;
        }

        float scale = UserScale * (Platform.IsTouchPrimary ? TouchScaleBoost : 1f);

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.referenceResolution = reference / Mathf.Max(0.01f, scale);
        scaler.matchWidthOrHeight = matchOverride >= 0f ? matchOverride : AutoMatch(reference);
    }

    /// <summary>1 (height) on screens at least as wide as the reference aspect, else 0 (width).</summary>
    public static float AutoMatch(Vector2 reference)
    {
        if (Screen.height <= 0 || reference.y <= 0f) return 0.5f;
        float screenAspect = (float)Screen.width / Screen.height;
        float refAspect = reference.x / reference.y;
        return screenAspect >= refAspect ? 1f : 0f;
    }
}
