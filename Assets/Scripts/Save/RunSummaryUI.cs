using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// End-of-run summary panel, shown on death (any layer) or victory (layer 9).
/// Lives in each gameplay scene on the UI canvas, panel disabled by default.
///
/// On Show: freezes time, disables player control, builds a RunStats snapshot,
/// records it to SaveManager (once), and displays the highlights.
/// "Back to Menu" returns to the main menu (which resets RunConfig).
///
/// Callers use RunSummaryUI.TryShow(victory) — returns false if no summary UI
/// exists in the scene, so callers can fall back to old behaviour (useful when
/// testing scenes without the full UI).
/// </summary>
public class RunSummaryUI : MonoBehaviour
{
    public static RunSummaryUI I { get; private set; }

    [Header("UI")]
    public GameObject panel;
    public TMP_Text titleText;
    public TMP_Text statsText;
    public Button menuButton;

    [Header("Titles")]
    public string victoryTitle = "HELL CONQUERED";
    public string deathTitle = "YOU DIED";

    private bool shown;

    // ================================================================
    //  LIFECYCLE
    // ================================================================

    private void Awake()
    {
        I = this;
        if (panel != null) panel.SetActive(false);
        if (menuButton != null) menuButton.onClick.AddListener(SceneFlow.GoToMainMenu);
    }

    private void OnDestroy()
    {
        if (I == this) I = null;
    }

    // ================================================================
    //  PUBLIC API
    // ================================================================

    /// <summary>Shows the summary if one exists in this scene. False otherwise.</summary>
    public static bool TryShow(bool victory)
    {
        if (I == null) return false;
        I.Show(victory);
        return true;
    }

    public void Show(bool victory)
    {
        if (shown) return;
        shown = true;

        PlayerStats stats = Object.FindFirstObjectByType<PlayerStats>();
        RunConfig cfg = RunConfig.I;

        string playerName = cfg != null ? cfg.playerName : "Sinner";
        int deepestLayer  = cfg != null ? cfg.currentLayer : 1;
        float duration    = cfg != null ? cfg.RunDuration : 0f;

        RunStats run = null;
        if (stats != null)
        {
            run = RunStatsBuilder.Build(stats, playerName, deepestLayer, duration, victory);

            // Loadout + max depth (banked layers + live tracker) for the leaderboard
            run.classId  = cfg != null ? cfg.selectedClassId  : "";
            run.weaponId = cfg != null ? cfg.selectedWeaponId : "";
            DepthTracker tracker = Object.FindFirstObjectByType<DepthTracker>();
            run.maxDepthReached = (cfg != null ? cfg.bankedDepth : 0f)
                                + (tracker != null ? tracker.CurrentDepth : 0f);

            SaveManager.RecordRun(run);
        }

        // Freeze the world, release the player
        Object.FindFirstObjectByType<PlayerStateController>()?.DisableControl();
        GameTime.Pause();

        if (titleText != null)
            titleText.text = victory ? victoryTitle : deathTitle;

        if (statsText != null)
            statsText.text = run != null ? FormatStats(run) : "";

        if (panel != null) panel.SetActive(true);
    }

    // ================================================================
    //  FORMATTING
    // ================================================================

    private static string FormatStats(RunStats r)
    {
        return
            $"<b>{r.playerName}</b>\n\n" +
            $"Layer reached:  {r.deepestLayer}\n" +
            $"Max depth:  {r.maxDepthReached:F0} m\n" +
            $"Level:  {r.finalLevel}\n" +
            $"Time:  {FormatDuration(r.runDurationSeconds)}\n\n" +
            $"Enemies killed:  {r.enemiesKilled}\n" +
            $"Damage dealt:  {r.totalDamageDealt:F0}\n" +
            $"Crits landed:  {r.critsLanded}\n" +
            $"Arrows fired:  {r.totalArrowsFired}\n\n" +
            $"Souls collected:  {r.soulsCollected}\n" +
            $"Damage taken:  {r.damageTaken:F0}";
    }

    private static string FormatDuration(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:00}:{s:00}";
    }
}
