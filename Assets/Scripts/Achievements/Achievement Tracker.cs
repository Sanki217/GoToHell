using UnityEngine;

/// <summary>
/// Polls single-run achievements against the live player during gameplay.
/// Put one in each gameplay scene (or on a persistent object).
/// Auto-finds PlayerStats. Cumulative + event achievements are handled elsewhere.
/// </summary>
public class AchievementTracker : MonoBehaviour
{
    [Tooltip("How often (seconds) to check single-run stat achievements.")]
    public float pollInterval = 0.5f;

    private PlayerStats stats;
    private float timer;

    private void Start()
    {
        stats = PlayerRefs.I != null ? PlayerRefs.I.Stats
                                     : Object.FindFirstObjectByType<PlayerStats>();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < pollInterval) return;
        timer = 0f;

        if (stats == null)
        {
            stats = PlayerRefs.I != null ? PlayerRefs.I.Stats
                                         : Object.FindFirstObjectByType<PlayerStats>();
            if (stats == null) return;
        }

        Achievements.CheckSingleRun(stats);
    }
}