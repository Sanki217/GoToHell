/// <summary>
/// Stats an achievement can track. Maps to fields on PlayerStats (live, single-run)
/// and RunStats (lifetime totals, cumulative).
/// Add new entries here and to the two readers in Achievements.cs as needed.
/// </summary>
public enum StatField
{
    EnemiesKilled,
    TotalDistance,
    SoulsCollected,
    TotalDamageDealt,
    CritsLanded,
    TotalArrowsFired,
    ArrowsHitEnemy,
    DashesHitEnemy,
    SlashesHitEnemy,
    JumpsPerformed,
    WallSlideCount,
    DashCount,
    HoverCount,
    BurnApplied,
    FreezeApplied,
    HolyApplied,
    ShockApplied,
    DamageTaken,
    XpGained,
    DeepestLayer,
    FinalLevel
}