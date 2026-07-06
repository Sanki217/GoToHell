using UnityEngine;

/// <summary>
/// Applies persisted graphics/settings defaults once at game start, before
/// any scene loads. Defaults live HERE, not scattered in UI scripts.
///
/// Current defaults: VSync OFF unless the player turned it on in settings.
/// </summary>
public static class GameSettingsBootstrap
{
    public const string VSyncPref = "gth_vsync";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        QualitySettings.vSyncCount = PlayerPrefs.GetInt(VSyncPref, 0);
    }
}
