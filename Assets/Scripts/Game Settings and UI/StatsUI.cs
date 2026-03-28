using UnityEngine;
using TMPro;

/// <summary>
/// TAB-toggled stats panel. Laid out in two columns to fit on screen.
///
/// Setup:
///   Create a Canvas Panel with TWO TMP_Text children side by side.
///   Wire leftStatsText and rightStatsText in Inspector.
///   Or use a single statsText — it will render as one block (legacy).
/// </summary>
public class StatsUI : MonoBehaviour
{
    [Header("References")]
    public GameObject player;
    public GameObject statsPanel;

    [Tooltip("Left column — Level, Combat, Ammo")]
    public TMP_Text leftStatsText;

    [Tooltip("Right column — Movement, Run History, Upgrades")]
    public TMP_Text rightStatsText;

    [Tooltip("Legacy single-text fallback — only used if left/right are not assigned")]
    public TMP_Text statsText;

    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.Tab;

    private PlayerStats playerStats;
    private PlayerLevelSystem levelSystem;
    private PlayerHealth playerHealth;
    private PlayerEnergy playerEnergy;
    private PlayerUpgradeManager upgradeManager;
    private bool isVisible = false;

    private void Start()
    {
        if (player == null)
            player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            playerStats = player.GetComponent<PlayerStats>();
            playerHealth = player.GetComponent<PlayerHealth>();
            playerEnergy = player.GetComponent<PlayerEnergy>();
            levelSystem = player.GetComponent<PlayerLevelSystem>();
            upgradeManager = player.GetComponent<PlayerUpgradeManager>();
        }

        if (statsPanel != null) statsPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
            if (statsPanel != null) statsPanel.SetActive(isVisible);
        }

        if (!isVisible || playerStats == null) return;

        if (leftStatsText != null && rightStatsText != null)
        {
            leftStatsText.text = BuildLeftColumn();
            rightStatsText.text = BuildRightColumn();
        }
        else if (statsText != null)
        {
            statsText.text = BuildLeftColumn() + "\n" + BuildRightColumn();
        }
    }

    // ================================================================
    //  LEFT COLUMN — Level, Combat, Ammo
    // ================================================================

    private string BuildLeftColumn()
    {
        var s = playerStats;
        var hp = playerHealth;

        // ── Level ────────────────────────────────────────────────────
        string left = "<b><color=#00FF99>══ LEVEL ══</color></b>\n";
        if (levelSystem != null)
        {
            bool maxed = levelSystem.CurrentLevel >= PlayerLevelSystem.MaxLevel;
            left +=
                $"Level:          {(maxed ? "MAX" : levelSystem.CurrentLevel.ToString())}\n" +
                $"Current XP:     {(maxed ? "—" : Mathf.FloorToInt(levelSystem.CurrentXP).ToString())}\n" +
                $"XP to Next:     {(maxed ? "—" : Mathf.FloorToInt(levelSystem.XPToNextLevel).ToString())}\n" +
                $"XP Multiplier:  {levelSystem.xpMultiplier:F2}×\n";
        }
        else left += "PlayerLevelSystem not found\n";
        left += "\n";

        // ── Combat ───────────────────────────────────────────────────
        left +=
            "<b><color=#FFD700>══ COMBAT ══</color></b>\n" +
            $"HP:             {(hp != null ? hp.CurrentHP : 0)} / {s.maxHP}\n" +
            $"Arrow Damage:   {s.arrowDamage:F1}\n" +
            $"Dash Damage:    {s.dashDamage:F1}\n" +
            $"Crit Chance:    {s.critChance * 100f:F1}%\n" +
            $"Crit Multi:     {s.critMultiplier * 100f:F0}%\n" +
            $"Knockback:      {s.knockbackForce:F1}\n" +
            $"Lifesteal:      {s.lifeSteal * 100f:F1}%\n" +
            $"Burn:           {s.burnStrength * 100f:F0}%\n" +
            $"Freeze:         {s.freezeStrength * 100f:F0}%\n" +
            $"Holy:           {s.holyStrength * 100f:F0}%\n" +
            $"Shock:          {s.shockStrength * 100f:F0}%\n" +
            "\n";

        // ── Ammo ─────────────────────────────────────────────────────
        left +=
            "<b><color=#FFD700>══ AMMO ══</color></b>\n" +
            $"Arrows:         {s.CurrentArrows} / {s.MaxArrows}\n" +
            "\n";

        // ── Active Upgrades ──────────────────────────────────────────
        left += "<b><color=#FF99FF>══ UPGRADES ══</color></b>\n";
        if (upgradeManager != null)
        {
            bool any = false;
            foreach (var kv in upgradeManager.GetActiveUpgrades())
            {
                left += $"  {kv.Key,-22} Lv{kv.Value}\n";
                any = true;
            }
            if (!any) left += "  None\n";
        }

        return left;
    }

    // ================================================================
    //  RIGHT COLUMN — Movement, Run History
    // ================================================================

    private string BuildRightColumn()
    {
        var s = playerStats;
        var en = playerEnergy;

        // ── Movement ─────────────────────────────────────────────────
        string right =
            "<b><color=#FFD700>══ MOVEMENT ══</color></b>\n" +
            $"Move Speed:     {s.moveSpeed:F1}\n" +
            $"Jump Force:     {s.jumpForce:F1}\n" +
            $"Max Jumps:      {s.maxJumps}\n" +
            $"Dash Distance:  {s.dashDistance:F1}\n" +
            $"Dash Cost:      {s.dashCost:F1}\n" +
            $"Dash Invinc:    {s.dashInvincibilityWindow:F2}s\n" +
            $"Max Energy:     {s.maxEnergy:F1}\n" +
            $"Energy:         {(en != null ? en.currentEnergy : 0f):F0}\n" +
            $"Hover Drain:    {s.hoverDrainRate:F1}/s\n" +
            $"Charge Drain:   {s.arrowChargeDrainRate:F1}/s\n" +
            $"Charge Time:    {s.arrowChargeDuration:F2}s\n" +
            $"Wall Slide:     {s.wallSlideSpeed:F1}\n" +
            $"Loot Range:     {s.lootRange:F1}\n" +
            $"Luck:           {s.luck:F1}\n" +
            "\n";

        // ── Run History ──────────────────────────────────────────────
        right +=
            "<b><color=#FF6666>══ HISTORY: MOVEMENT ══</color></b>\n" +
            $"Distance:       {s.totalDistance:F0}m\n" +
            $"Jumps:          {s.jumpsPerformed}\n" +
            $"Wall Slides:    {s.wallSlideCount} ({s.totalWallSlideDuration:F1}s)\n" +
            $"Hovers:         {s.hoverCount} ({s.totalHoverDuration:F1}s)\n" +
            $"Dashes:         {s.dashCount} (E:{s.dashesHitEnemy} W:{s.dashesHitWall})\n" +
            "\n" +
            "<b><color=#FF6666>══ HISTORY: COMBAT ══</color></b>\n" +
            $"Arrows Fired:   {s.totalArrowsFired} (L:{s.weakArrowsFired} M:{s.mediumArrowsFired} S:{s.chargedArrowsFired})\n" +
            $"Arrows Hit:     {s.arrowsHitEnemy}\n" +
            $"Kills:          {s.enemiesKilled}\n" +
            $"Dmg Dealt:      {s.totalDamageDealt:F0}\n" +
            $"Crits:          {s.critsLanded}\n" +
            $"Status Applied: B:{s.burnApplied} F:{s.freezeApplied} H:{s.holyApplied} S:{s.shockApplied}\n" +
            "\n" +
            "<b><color=#FF6666>══ HISTORY: RESOURCES ══</color></b>\n" +
            $"Souls:          {s.soulsCollected}\n" +
            $"XP:             {s.xpGained:F0}\n" +
            $"Energy Gained:  {s.energyGainedTotal:F0}\n" +
            $"  Kills:        {s.energyFromKills:F0}\n" +
            $"  Falling:      {s.energyFromFalling:F0}\n" +
            $"  Lava:         {s.energyFromLava:F0}\n" +
            $"  Wall Slide:   {s.energyFromWallSlide:F0}\n" +
            "\n" +
            "<b><color=#FF6666>══ HISTORY: SURVIVAL ══</color></b>\n" +
            $"Dmg Taken:      {s.damageTaken:F0}\n" +
            $"Times Hit:      {s.timesHit}\n" +
            $"HP Restored:    {s.hpRestored:F0}\n" +
            $"Layers Done:    {s.layersCompleted}\n";

        return right;
    }
}