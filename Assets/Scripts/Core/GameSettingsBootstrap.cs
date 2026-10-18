/// <summary>
/// Kept for compatibility: older code referenced the VSync PlayerPrefs key
/// here. All settings now live in GameSettings (which applies them at boot).
/// </summary>
public static class GameSettingsBootstrap
{
    public const string VSyncPref = "gth_vsync";
}
