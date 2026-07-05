using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Single source of truth for scene names and transitions.
/// Every scene change in the game goes through here, so scene names
/// never get hard-coded across multiple button scripts.
///
/// Scene names must match the scene file names in Build Settings exactly.
/// </summary>
public static class SceneFlow
{
    // ── Scene name constants ────────────────────────────────────────
    public const string MainMenu         = "Main Menu";
    public const string CharacterCreator = "Character Creator";
    public const string Collection       = "Collection";

    // Gameplay layers are "Level 1" ... "Level 9"
    public const string LevelPrefix = "Level ";
    public const int    MaxLayer    = 9;

    // ── Transitions ─────────────────────────────────────────────────

    public static void GoToMainMenu()
    {
        RunManager.EndRun();   // resets run-scoped state + time
        SceneManager.LoadScene(MainMenu);
    }

    public static void GoToCharacterCreator()
    {
        GameTime.Resume();
        SceneManager.LoadScene(CharacterCreator);
    }

    public static void GoToCollection()
    {
        GameTime.Resume();
        SceneManager.LoadScene(Collection);
    }

    /// <summary>Loads a specific gameplay layer (1–9).</summary>
    public static void GoToLevel(int layer)
    {
        GameTime.Resume();
        layer = Mathf.Clamp(layer, 1, MaxLayer);
        SceneManager.LoadScene(LevelPrefix + layer);
    }

    /// <summary>Starts a fresh run at Level 1. RunManager resets all run-scoped state.</summary>
    public static void StartRunAtLevel1()
    {
        RunManager.BeginRun();
        SceneManager.LoadScene(LevelPrefix + "1");
    }

    /// <summary>Advances to the next layer, updating RunConfig.currentLayer.</summary>
    public static void GoToNextLayer()
    {
        GameTime.Resume();
        int next = (RunConfig.I != null ? RunConfig.I.currentLayer : 1) + 1;
        next = Mathf.Clamp(next, 1, MaxLayer);
        if (RunConfig.I != null) RunConfig.I.currentLayer = next;
        SceneManager.LoadScene(LevelPrefix + next);
    }

    public static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
