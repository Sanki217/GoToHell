using UnityEngine;

/// <summary>
/// Character level tuning: how much XP a run gives and how much each level
/// needs. One asset at Assets/Resources/CharacterProgressionConfig
/// (Tools → Go To Hell → 1. Create Default Assets makes it); without it the
/// defaults below are used.
///
/// XP per run = kills × xpPerKill + layers reached × xpPerLayer
///            + depth (m) × xpPerMeter + (victory ? xpForVictory : 0)
/// XP to go from level L to L+1 = xpBase + xpPerLevel × (L − 1)
/// </summary>
[CreateAssetMenu(menuName = "GoToHell/Character Progression Config")]
public class CharacterProgressionConfig : ScriptableObject
{
    public const string ResourcePath = "CharacterProgressionConfig";

    [Header("XP from a run")]
    public float xpPerKill = 1f;
    public float xpPerLayer = 20f;
    public float xpPerMeter = 0.05f;
    public float xpForVictory = 150f;

    [Header("XP needed per level")]
    public int xpBase = 100;
    public int xpPerLevel = 50;
    public int maxLevel = 50;

    public int XpForRun(RunStats run)
    {
        if (run == null) return 0;
        float xp = run.enemiesKilled * xpPerKill
                 + Mathf.Max(1, run.deepestLayer) * xpPerLayer
                 + run.maxDepthReached * xpPerMeter
                 + (run.victory ? xpForVictory : 0f);
        return Mathf.Max(0, Mathf.RoundToInt(xp));
    }

    public int XpToNext(int level) => Mathf.Max(1, xpBase + xpPerLevel * Mathf.Max(0, level - 1));

    private static CharacterProgressionConfig cached;

    /// <summary>The project's config, or a default instance.</summary>
    public static CharacterProgressionConfig Get()
    {
        if (cached != null) return cached;
        cached = Resources.Load<CharacterProgressionConfig>(ResourcePath);
        if (cached == null) cached = CreateInstance<CharacterProgressionConfig>();
        return cached;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics() => cached = null;
}
