using UnityEngine;

public class UpgradeBowExtraArrow : PlayerUpgrade
{
    public override string Id => "Bow_ExtraArrow";

    // ================================================================
    //  This upgrade owns its own arrow prefab.
    //  Assign it in the UpgradeOrb prefab's inspector,
    //  or wire it up however the upgrade pickup system delivers it.
    //
    //  PlayerShooting knows nothing about extra arrows —
    //  this upgrade handles everything itself.
    // ================================================================

    [Header("Extra Arrow Settings")]
    public GameObject extraArrowPrefab;   // assign in Inspector on the upgrade prefab
    public float spreadAngleDegrees = 5f; // angle between each extra arrow

    private int level;
    private PlayerShooting shooting;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        shooting = mgr.GetComponent<PlayerShooting>();

        mgr.OnWeakArrowFired += OnArrowFired;
        mgr.OnMediumArrowFired += OnArrowFired;
        mgr.OnChargedArrowFired += OnArrowFired;
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        level = newLevel;
    }

    private void OnArrowFired(Vector3 dir, float speedMultiplier)
    {
        if (shooting == null || extraArrowPrefab == null)
            return;

        // Level 1 = 1 extra arrow, level 2 = 2 extra arrows, etc.
        for (int i = 0; i < level; i++)
        {
            float angle = GetSpreadAngle(i + 1);
            Vector3 newDir = Quaternion.Euler(0, 0, angle) * dir;

            // Use PlayerShooting.SpawnArrow with our own prefab
            // consumeAmmo: false — extra arrows are free
            shooting.SpawnArrow(extraArrowPrefab, newDir, speedMultiplier, consumeAmmo: false);

            // Fire the extra arrow event so other upgrades can react
            shooting.GetComponent<PlayerUpgradeManager>()?.FireExtraArrow(newDir, speedMultiplier);
        }
    }

    private float GetSpreadAngle(int index)
    {
        // Alternates: +5, -5, +10, -10, +15, -15 ...
        int step = (index + 1) / 2;
        float angle = step * spreadAngleDegrees;
        if (index % 2 == 0) angle = -angle;
        return angle;
    }
}