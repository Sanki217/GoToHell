using UnityEngine;

/// <summary>
/// Single owner of Time.timeScale and Time.fixedDeltaTime.
/// NOTHING else in the project writes those two values directly — always go
/// through Pause/Resume/SetScale. This guarantees fixedDeltaTime stays in
/// sync with timeScale (slow-mo) and that no system leaves the game frozen
/// or in permanent slow motion after a transition.
/// </summary>
public static class GameTime
{
    /// <summary>Project default fixed timestep (matches Project Settings).</summary>
    public const float BaseFixedDelta = 0.02f;

    public static bool IsPaused => Time.timeScale == 0f;

    /// <summary>Freeze gameplay (menus, pickers, summaries). UI keeps running on unscaled time.</summary>
    public static void Pause()
    {
        Time.timeScale = 0f;
    }

    /// <summary>Back to normal speed. Safe to call from any state.</summary>
    public static void Resume()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = BaseFixedDelta;
    }

    /// <summary>
    /// Slow motion (charge shots, resurrection). Keeps physics in sync by
    /// scaling fixedDeltaTime with the timescale.
    /// </summary>
    public static void SetScale(float scale)
    {
        scale = Mathf.Clamp(scale, 0.01f, 1f);
        Time.timeScale = scale;
        Time.fixedDeltaTime = BaseFixedDelta * scale;
    }
}
