using UnityEngine;

public enum AchievementType
{
    Cumulative,   // checked against lifetime totals across all runs
    SingleRun,    // checked against the current run's live stats
    Event         // triggered by code (e.g. "completed level 1 for the first time")
}

/// <summary>
/// One achievement. Completing it unlocks one pact.
/// Create via: Assets → Create → GoToHell → Achievement
/// Place the asset under Assets/Resources/Achievements/ so it's auto-loaded.
/// </summary>
[CreateAssetMenu(menuName = "GoToHell/Achievement")]
public class AchievementDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Stable unique ID used in save data. Never change once shipped.")]
    public string id;
    public string displayName;
    [TextArea(2, 4)] public string description;
    public Sprite icon;

    [Header("Type")]
    public AchievementType type;

    [Header("Cumulative / SingleRun — stat threshold")]
    public StatField statField;
    public float threshold = 100f;

    [Header("Event — id to match (only for Event type)")]
    public string eventId;

    [Header("Rewards (any combination, all optional)")]
    [Tooltip("Pact unlocked when this achievement completes.")]
    public PactDefinition pactToUnlock;

    [Tooltip("Class unlocked when this achievement completes (e.g. Warrior on first death).")]
    public ClassDefinition classToUnlock;

    [Tooltip("Weapon unlocked when this achievement completes (e.g. Sword on finishing Level 1).")]
    public WeaponDefinition weaponToUnlock;
}