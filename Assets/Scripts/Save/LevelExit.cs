using UnityEngine;

/// <summary>
/// Place at the end of a level as a trigger volume. When the player enters,
/// it fires the "level_N_complete" achievement event and advances to the next layer.
/// On the final layer it counts as victory (handled by the finish flow in Step 6;
/// for now it goes to the main menu).
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

        int layer = RunConfig.I != null ? RunConfig.I.currentLayer : 1;

        // Fire achievement event: "level_1_complete", "level_2_complete", ...
        Achievements.TriggerEvent($"level_{layer}_complete");

        if (layer >= SceneFlow.MaxLayer)
        {
            // Final layer cleared — victory. Finish screen comes in Step 6.
            SceneFlow.GoToMainMenu();
        }
        else
        {
            SceneFlow.GoToNextLayer();
        }
    }
}