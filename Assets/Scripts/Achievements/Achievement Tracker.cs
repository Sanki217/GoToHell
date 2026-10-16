using UnityEngine;

/// <summary>
/// Polls single-run achievements against the live player during gameplay.
/// Added to the player automatically by PlayerRefs — nothing to place in
/// scenes. (A manually placed one still works; checks are idempotent.)
/// </summary>
public class AchievementTracker : MonoBehaviour
{
    [Tooltip("How often (seconds, unscaled) to check single-run stat achievements.")]
    public float pollInterval = 0.5f;

    private PlayerStats stats;
    private float timer;

    private void Update()
    {
        timer += Time.unscaledDeltaTime;
        if (timer < pollInterval) return;
        timer = 0f;

        if (stats == null)
        {
            if (!TryGetComponent(out stats))
                stats = PlayerRefs.I != null ? PlayerRefs.I.Stats : null;
            if (stats == null) return;
        }

        Achievements.CheckSingleRun(stats);
    }
}
