using UnityEngine;

/// <summary>
/// Global meta-progression tuning: what the meta currency is called, how much
/// a run pays out.
///
/// Create via: Assets → Create → GoToHell → Meta Progression Config
/// Store as:   Assets/Resources/MetaProgressionConfig.asset (exact name)
/// Missing asset = these defaults.
/// </summary>
[CreateAssetMenu(menuName = "GoToHell/Meta Progression Config")]
public class MetaProgressionConfig : ScriptableObject
{
    [Header("Currency")]
    public string currencyName = "Brimstone";
    public Sprite currencyIcon;

    [Header("Run Payout (all summed, then multiplied)")]
    public float perEnemyKilled = 0.25f;
    public float perLayerReached = 10f;
    public float perLayerCompleted = 15f;
    public float perPlayerLevel = 2f;
    public float perMinuteSurvived = 1f;
    public int victoryBonus = 100;

    /// <summary>Payout for a finished run, before RunModifiers' meta-currency bonus.</summary>
    public float BasePayout(RunStats run)
    {
        if (run == null) return 0f;
        float v = run.enemiesKilled * perEnemyKilled
                + Mathf.Max(1, run.deepestLayer) * perLayerReached
                + run.layersCompleted * perLayerCompleted
                + run.finalLevel * perPlayerLevel
                + run.runDurationSeconds / 60f * perMinuteSurvived;
        if (run.victory) v += victoryBonus;
        return v;
    }
}
