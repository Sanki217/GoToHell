using UnityEngine;

/// <summary>
/// One place to ask "what are we running on?" so platform branches never get
/// scattered (`#if UNITY_ANDROID` in gameplay code is how ports turn painful).
///
/// Simulate mobile on desktop: tick "Simulate Mobile" via
/// Tools → Go To Hell → Mobile → Toggle Simulate Mobile (editor only), or set
/// Platform.SimulateMobile = true from a debug menu. Combine with the Device
/// Simulator window to preview safe areas and screen sizes.
/// </summary>
public static class Platform
{
    private const string SimulatePrefKey = "platform.simulateMobile";

    /// <summary>Phone / tablet build, or mobile simulation turned on.</summary>
    public static bool IsMobile => Application.isMobilePlatform || SimulateMobile;

    /// <summary>A touchscreen is the main input (show on-screen controls, bigger hit areas).</summary>
    public static bool IsTouchPrimary => IsMobile;

    /// <summary>Editor/dev toggle to run the mobile code paths on desktop.</summary>
    public static bool SimulateMobile
    {
        get => Debug.isDebugBuild && PlayerPrefs.GetInt(SimulatePrefKey, 0) == 1;
        set { PlayerPrefs.SetInt(SimulatePrefKey, value ? 1 : 0); PlayerPrefs.Save(); }
    }
}
