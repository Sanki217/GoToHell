/// <summary>
/// Stats an achievement can track. Maps to RunStats fields — both the live
/// run (PlayerStats.history) and lifetime totals use the same type.
/// Add new entries here and one line to Achievements.GetStat.
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