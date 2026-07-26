using UnityEngine;

/// <summary>
/// Place at the end of a level as a trigger volume. When the player enters,
/// the layer completes — see CompleteLevel(). Kept for levels that still end
/// by walking through; LevelExitTarget ends the layer by shooting instead.
///
/// The current layer is read from RunConfig.currentLayer.
/// </summary>
public class LevelExit : MonoBehaviour
{
    private bool used = false;

    private void Reset()
    {
        Collider c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (used) return;
        if (!other.CompareTag("Player")) return;
        used = true;

        CompleteLevel();
    }

    /// <summary>
    /// Ends the current layer: fires the "level_N_complete" achievement event,
    /// then shows the victory summary on the final layer (falling back to the
    /// main menu), or banks depth and advances to the next layer.
    /// </summary>
    public static void CompleteLevel()
    {
        int layer = RunConfig.I != null ? RunConfig.I.currentLayer : 1;

        // Fire achievement event: "level_1_complete", "level_2_complete", ...
        Achievements.TriggerEvent($"level_{layer}_complete");

        // Demo-safe: if the next layer's scene isn't in Build Settings yet,
        // the run ends here as a victory instead of crashing on a missing scene.
        bool isFinalLayer = layer >= SceneFlow.MaxLayer;
        bool nextLevelExists = Application.CanStreamedLevelBeLoaded(SceneFlow.LevelPrefix + (layer + 1));

        if (isFinalLayer || !nextLevelExists)
        {
            // Run complete — victory summary, fall back to menu.
            // (Depth is NOT banked here — RunSummaryUI reads the live tracker.)
            if (!RunSummaryUI.TryShow(true))
                SceneFlow.GoToMainMenu();
        }
        else
        {
            // Bank this layer's depth before the scene (and its DepthTracker) unloads.
            DepthTracker tracker = Object.FindFirstObjectByType<DepthTracker>();
            if (tracker != null && RunConfig.I != null)
                RunConfig.I.bankedDepth += tracker.CurrentDepth;

            SceneFlow.GoToNextLayer();
        }
    }
}
