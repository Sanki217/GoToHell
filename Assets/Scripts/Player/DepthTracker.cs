using UnityEngine;

/// <summary>
/// Tracks the player's maximum depth in the current layer: the lowest Y ever
/// reached below the spawn point. Falling, climbing back up, and falling the
/// same stretch again does NOT count twice — only new lowest-Y extends depth.
///
/// Total run depth = RunConfig.bankedDepth (previous layers, banked by
/// LevelExit) + CurrentDepth (this layer). Read at run end by RunSummaryUI.
///
/// Put on the Player root. Always enabled — class/weapon independent.
/// </summary>
public class DepthTracker : MonoBehaviour
{
    private float startY;
    private float lowestY;

    /// <summary>Depth reached in THIS layer so far (≥ 0, world units).</summary>
    public float CurrentDepth => Mathf.Max(0f, startY - lowestY);

    private void Start()
    {
        startY = transform.position.y;
        lowestY = startY;
    }

    private void FixedUpdate()
    {
        float y = transform.position.y;
        if (y < lowestY) lowestY = y;
    }

    /// <summary>Total depth across the whole run, including banked layers.</summary>
    public float TotalRunDepth =>
        (RunConfig.I != null ? RunConfig.I.bankedDepth : 0f) + CurrentDepth;
}
