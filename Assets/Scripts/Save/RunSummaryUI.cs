using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// End-of-run summary panel, shown on death (any layer) or victory (layer 9).
/// Lives in each gameplay scene on the UI canvas, panel disabled by default.
///
/// On Show: records the run through RunManager.CompleteRun (idempotent),
/// freezes time, disables player control, and displays the highlights.
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

        // Records the run once (history, best, lifetime, cumulative achievements)
        RunStats run = RunManager.CompleteRun(victory);

        // Freeze the world, release the player
        if (PlayerRefs.I != null && PlayerRefs.I.StateCtrl != null)
            PlayerRefs.I.StateCtrl.DisableControl();
        GameTime.Pause(this);

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
            $"Damage taken:  {r.damageTaken:F0}" +
            (r.hardcore && !r.victory ? "\n\n<color=#FF4444><b>HARDCORE — this character is gone.</b></color>" : "") +
            $"\n\n<color=#FFB347><b>+{r.metaCurrencyEarned} {MetaProgression.CurrencyName}</b></color>";
    }

    private static string FormatDuration(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:00}:{s:00}";
    }
}
