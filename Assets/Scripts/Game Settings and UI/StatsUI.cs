using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// TAB-toggled two-column stats panel.
/// Left:  Primary Stats, Level, Movement, Combat, Ammo
/// Right: Active Upgrades, Run History
///
/// HOVER TOOLTIPS:
///   - Hover on a primary stat name → shows which secondary stats it affects.
///   - Hover on an upgrade name   → shows the full upgrade card (rarity, description, stat bonuses).
///
/// Both use TMP &lt;link&gt; tags and TMP_TextUtilities.FindIntersectingLink
/// so they work on pure text without spawning extra GameObjects.
///
/// TOOLTIP SETUP:
///   1. Create a child Panel under statsPanel named "Tooltip".
///   2. Add a TMP_Text child named "TooltipText".
///   3. Optionally add an Image child named "TooltipBG" (CanvasGroup for fade).
///   4. Wire tooltipPanel + tooltipText in the Inspector.
///   5. Tooltip auto-follows the mouse and shows on hover.
/// </summary>
public class StatsUI : MonoBehaviour
{
    [Header("References")]
    public GameObject player;
    public GameObject statsPanel;

    [Tooltip("Left column — Primary Stats, Level, Movement, Combat, Ammo")]
    public TMP_Text leftStatsText;

    [Tooltip("Right column — Active Upgrades, Run History")]
    public TMP_Text rightStatsText;

    [Tooltip("Legacy fallback — only used if left/right are not assigned")]
    public TMP_Text statsText;

    [Header("Tooltip")]
    [Tooltip("Panel that shows the hover tooltip. Auto-hidden when not hovered.")]
    public GameObject tooltipPanel;
    [Tooltip("Text element inside the tooltip panel.")]
    public TMP_Text tooltipText;

    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.Tab;

    // Components
    private PlayerStats playerStats;
    private PlayerLevelSystem levelSystem;
    private PlayerHealth playerHealth;
    private PlayerEnergy playerEnergy;
    private PlayerUpgradeManager upgradeManager;
    private bool isVisible = false;

    // Tooltip state
    private RectTransform tooltipRect;
    private Canvas rootCanvas;
    private string lastHoveredLink = "";

    // Base values — snapshot on Start, before any upgrades apply
    private int baseMaxHP;
    private float baseArrowDamage, baseSlashDamage, baseDashDamage, baseCritChance, baseCritMultiplier;
    private float baseKnockback, baseLifesteal;
    private float baseBurn, baseFreeze, baseHoly, baseShock;
    private float baseMoveSpeed, baseJumpForce, baseDashDist, baseDashCost;
    private float baseDashInvinc, baseMaxEnergy, baseHoverDrain;
    private float baseChargeDrain, baseChargeDur, baseWallSlide, baseLootRange, baseLuck;
    private int baseMaxArrows;

    private void Start()
    {
        var refs = PlayerRefs.I;
        if (player == null && refs != null) player = refs.gameObject;

        if (refs != null)
        {
            playerStats = refs.Stats;
            playerHealth = refs.Health;
            playerEnergy = refs.Energy;
            levelSystem = refs.LevelSystem;
            upgradeManager = refs.Upgrades;
        }

        if (statsPanel != null) statsPanel.SetActive(false);
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
            tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        }

        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;

        // Snapshot base values before any upgrades are applied
        if (playerStats != null) SnapshotBase();
    }

    private void SnapshotBase()
    {
        var s = playerStats;
        baseMaxHP = s.maxHP;
        baseArrowDamage = s.arrowDamage;
        baseSlashDamage = s.slashDamage;
        baseDashDamage = s.dashDamage;
        baseCritChance = s.critChance;
        baseCritMultiplier = s.critMultiplier;
        baseKnockback = s.knockbackForce;
        baseLifesteal = s.lifeSteal;
        baseBurn = s.burnStrength;
        baseFreeze = s.freezeStrength;
        baseHoly = s.holyStrength;
        baseShock = s.shockStrength;
        baseMoveSpeed = s.moveSpeed;
        baseJumpForce = s.jumpForce;
        baseDashDist = s.dashDistance;
        baseDashCost = s.dashCost;
        baseDashInvinc = s.dashInvincibilityWindow;
        baseMaxEnergy = s.maxEnergy;
        baseHoverDrain = s.hoverDrainRate;
        baseChargeDrain = s.arrowChargeDrainRate;
        baseChargeDur = s.arrowChargeDuration;
        baseWallSlide = s.wallSlideSpeed;
        baseLootRange = s.lootRange;
        baseLuck = s.luck;
        baseMaxArrows = s.MaxArrows;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
            if (statsPanel != null) statsPanel.SetActive(isVisible);
            if (!isVisible && tooltipPanel != null) tooltipPanel.SetActive(false);
        }

        if (!isVisible || playerStats == null) return;

        if (leftStatsText != null && rightStatsText != null)
        {
            leftStatsText.text = BuildLeft();
            rightStatsText.text = BuildRight();
        }
        else if (statsText != null)
        {
            statsText.text = BuildLeft() + "\n" + BuildRight();
        }

        HandleHover();
    }

    // ================================================================
    //  HOVER DETECTION — TMP link tags
    // ================================================================

    private void HandleHover()
    {
        if (tooltipPanel == null || tooltipText == null) return;

        string hoveredLink = DetectLink(leftStatsText);
        if (string.IsNullOrEmpty(hoveredLink))
            hoveredLink = DetectLink(rightStatsText);

        if (!string.IsNullOrEmpty(hoveredLink))
        {
            if (hoveredLink != lastHoveredLink)
            {
                lastHoveredLink = hoveredLink;
                tooltipText.text = BuildTooltipContent(hoveredLink);
                tooltipPanel.SetActive(true);
            }
            PositionTooltip();
        }
        else
        {
            if (tooltipPanel.activeSelf)
            {
                tooltipPanel.SetActive(false);
                lastHoveredLink = "";
            }
        }
    }

    private string DetectLink(TMP_Text textComponent)
    {
        if (textComponent == null) return null;

        // Force mesh update so link info is current
        textComponent.ForceMeshUpdate();

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(
            textComponent, Input.mousePosition, null);

        if (linkIndex >= 0 && linkIndex < textComponent.textInfo.linkCount)
            return textComponent.textInfo.linkInfo[linkIndex].GetLinkID();

        return null;
    }

    private void PositionTooltip()
    {
        if (tooltipRect == null || rootCanvas == null) return;

        Vector2 mousePos = Input.mousePosition;
        Vector2 offset = new Vector2(15f, -15f);

        // Keep tooltip on screen
        float w = tooltipRect.rect.width * rootCanvas.scaleFactor;
        float h = tooltipRect.rect.height * rootCanvas.scaleFactor;

        if (mousePos.x + offset.x + w > Screen.width)
            offset.x = -w - 15f;
        if (mousePos.y + offset.y - h < 0f)
            offset.y = h + 15f;

        tooltipRect.position = mousePos + offset;
    }

    // ================================================================
    //  TOOLTIP CONTENT
    // ================================================================

    private string BuildTooltipContent(string linkId)
    {
        // Primary stat tooltip — linkId starts with "stat_"
        if (linkId.StartsWith("stat_"))
        {
            string statName = linkId.Substring(5);
            return GetPrimaryStatTooltip(statName);
        }

        // Upgrade tooltip — linkId is the upgrade's UpgradeId
        return GetUpgradeTooltip(linkId);
    }

    private string GetPrimaryStatTooltip(string statName)
    {
        switch (statName)
        {
            case "Agility":
                return "<b><color=#FF9933>Agility</color></b>\n\n" +
                       "Affects:\n" +
                       "  Move Speed\n" +
                       "  Jump Force\n" +
                       "  Dash Distance\n" +
                       "  Wall Slide Speed";
            case "AttackDamage":
                return "<b><color=#FF4444>Attack Damage</color></b>\n\n" +
                       "Affects:\n" +
                       "  Arrow Damage\n" +
                       "  Slash Damage\n" +
                       "  Dash Damage\n" +
                       "  Crit Multiplier\n" +
                       "  Knockback Force";
            case "Luck":
                return "<b><color=#AAFF44>Luck</color></b>\n\n" +
                       "Affects:\n" +
                       "  Crit Chance\n" +
                       "  Upgrade Rarity Rolls\n" +
                       "  Stat Bonus Ranges\n" +
                       "  Loot Range";
            case "Psyche":
                return "<b><color=#FF66CC>Psyche</color></b>\n\n" +
                       "Affects:\n" +
                       "  Burn Strength\n" +
                       "  Freeze Strength\n" +
                       "  Holy Strength\n" +
                       "  Shock Strength\n" +
                       "  Lifesteal\n" +
                       "  Ability Damage Scaling";
            case "Health":
                return "<b><color=#44FF88>Health</color></b>\n\n" +
                       "Affects:\n" +
                       "  Max HP\n" +
                       "  Max Energy";
            case "Size":
                return "<b><color=#AAAAAA>Size</color></b>\n\n" +
                       "Affects:\n" +
                       "  AoE Radius\n" +
                       "  Gravity Arrow Pull Radius\n" +
                       "  Explosion Radius";
            case "Cooldown":
                return "<b><color=#44FFEE>Cooldown</color></b>\n\n" +
                       "Affects:\n" +
                       "  Charge Duration\n" +
                       "  Charge Drain Rate\n" +
                       "  Hover Drain Rate\n" +
                       "  Dash Cost";
            default:
                return statName;
        }
    }

    private string GetUpgradeTooltip(string upgradeId)
    {
        if (upgradeManager == null) return upgradeId;

        PlayerUpgrade upgrade = upgradeManager.GetUpgrade(upgradeId);
        AppliedUpgradeInfo info = upgradeManager.GetUpgradeInfo(upgradeId);

        if (upgrade == null) return upgradeId;

        // Rarity color
        UpgradeRarity rarity = info != null ? info.rarity : upgrade.rarity;
        Color rarityColor = UpgradeRarityRoller.GetRarityColor(rarity);
        string rarityHex = ColorUtility.ToHtmlStringRGB(rarityColor);
        string rarityName = rarity.ToString().ToUpper();

        // Build tooltip
        string tip = $"<b><color=#{rarityHex}>{upgrade.displayName}</color></b>\n";
        tip += $"<color=#{rarityHex}>{rarityName}</color>  <color=#888888>{upgrade.category}</color>\n\n";

        // Dynamic description with current stats
        string desc = upgrade.GetDynamicDescription(playerStats, null);
        if (string.IsNullOrEmpty(desc)) desc = upgrade.description;
        tip += desc;

        // Stat bonuses
        if (info?.statBonuses != null && info.statBonuses.Count > 0)
        {
            tip += "\n\n<color=#AAFFAA>Stat Bonuses:</color>\n";
            foreach (var bonus in info.statBonuses)
                tip += $"  <color=#00FF88>+{bonus.value:F1}</color> {UpgradeStatBonus.GetName(bonus.stat)}\n";
        }

        return tip;
    }

    // ================================================================
    //  HELPERS — stat display with base + bonus
    // ================================================================

    private string Stat(string label, float current, float baseVal, string fmt = "F1")
    {
        string baseStr = current.ToString(fmt);
        float bonus = current - baseVal;
        if (Mathf.Abs(bonus) < 0.001f)
            return $"{label,-18}<color=#FFFFFF>{baseStr}</color>\n";
        string bonusStr = bonus > 0
            ? $"<color=#00FF66>(+{bonus.ToString(fmt)})</color>"
            : $"<color=#FF4444>({bonus.ToString(fmt)})</color>";
        return $"{label,-18}<color=#FFFFFF>{baseStr}</color> {bonusStr}\n";
    }

    private string StatPct(string label, float current, float baseVal)
    {
        string baseStr = $"{current * 100f:F0}%";
        float bonus = current - baseVal;
        if (Mathf.Abs(bonus) < 0.0001f)
            return $"{label,-18}<color=#FFFFFF>{baseStr}</color>\n";
        string bonusStr = bonus > 0
            ? $"<color=#00FF66>(+{bonus * 100f:F0}%)</color>"
            : $"<color=#FF4444>({bonus * 100f:F0}%)</color>";
        return $"{label,-18}<color=#FFFFFF>{baseStr}</color> {bonusStr}\n";
    }

    private string StatInt(string label, int current, int baseVal)
    {
        int bonus = current - baseVal;
        if (bonus == 0)
            return $"{label,-18}<color=#FFFFFF>{current}</color>\n";
        string bonusStr = bonus > 0
            ? $"<color=#00FF66>(+{bonus})</color>"
            : $"<color=#FF4444>({bonus})</color>";
        return $"{label,-18}<color=#FFFFFF>{current}</color> {bonusStr}\n";
    }

    /// <summary>Wraps a primary stat label in a TMP link tag for hover detection.</summary>
    private string LinkedStat(string label, string linkId, float current, float baseVal, string fmt = "F1")
    {
        string linked = $"<link=\"stat_{linkId}\">{label}</link>";
        string baseStr = current.ToString(fmt);
        float bonus = current - baseVal;
        if (Mathf.Abs(bonus) < 0.001f)
            return $"{linked,-18}<color=#FFFFFF>{baseStr}</color>\n";
        string bonusStr = bonus > 0
            ? $"<color=#00FF66>(+{bonus.ToString(fmt)})</color>"
            : $"<color=#FF4444>({bonus.ToString(fmt)})</color>";
        return $"{linked,-18}<color=#FFFFFF>{baseStr}</color> {bonusStr}\n";
    }

    // ================================================================
    //  LEFT COLUMN — Level, Movement, Combat, Ammo
    // ================================================================

    private string BuildLeft()
    {
        var s = playerStats;
        var hp = playerHealth;

        // -- Primary Stats (hoverable) --
        string left = "<b><color=#FF88FF>== PRIMARY STATS ==</color></b>\n";
        left += LinkedStat("Agility", "Agility", s.agility, 0f);
        left += LinkedStat("Atk Damage", "AttackDamage", s.attackDamage, 0f);
        left += LinkedStat("Luck", "Luck", s.luck, 0f);
        left += LinkedStat("Psyche", "Psyche", s.psyche, 0f);
        left += LinkedStat("Health", "Health", s.health_stat, 0f);
        left += LinkedStat("Size", "Size", s.size, 0f);
        left += LinkedStat("Cooldown", "Cooldown", s.cooldown, 0f);
        left += "\n";

        // -- Level --
        left += "<b><color=#00FF99>== LEVEL ==</color></b>\n";
        if (levelSystem != null)
        {
            bool maxed = levelSystem.CurrentLevel >= PlayerLevelSystem.MaxLevel;
            left += $"{"Level",-18}<color=#FFFFFF>{(maxed ? "MAX" : levelSystem.CurrentLevel.ToString())}</color>\n";
            left += $"{"Current XP",-18}<color=#FFFFFF>{(maxed ? "\u2014" : Mathf.FloorToInt(levelSystem.CurrentXP).ToString())}</color>\n";
            left += $"{"XP to Next",-18}<color=#FFFFFF>{(maxed ? "\u2014" : Mathf.FloorToInt(levelSystem.XPToNextLevel).ToString())}</color>\n";
            left += $"{"XP Multiplier",-18}<color=#FFFFFF>{levelSystem.xpMultiplier:F2}x</color>\n";
        }
        left += "\n";

        // -- Movement --
        left += "<b><color=#FFD700>== MOVEMENT ==</color></b>\n";
        left += Stat("Move Speed", s.moveSpeed, baseMoveSpeed);
        left += Stat("Jump Force", s.jumpForce, baseJumpForce);
        left += Stat("Dash Distance", s.dashDistance, baseDashDist);
        left += Stat("Dash Cost", s.dashCost, baseDashCost);
        left += Stat("Dash Invinc", s.dashInvincibilityWindow, baseDashInvinc, "F2");
        left += Stat("Max Energy", s.maxEnergy, baseMaxEnergy);
        left += $"{"Energy",-18}<color=#FFFFFF>{(playerEnergy != null ? playerEnergy.currentEnergy : 0f):F0}</color>\n";
        left += Stat("Hover Drain/s", s.hoverDrainRate, baseHoverDrain);
        left += Stat("Charge Drain/s", s.arrowChargeDrainRate, baseChargeDrain);
        left += Stat("Charge Time", s.arrowChargeDuration, baseChargeDur, "F2");
        left += Stat("Wall Slide", s.wallSlideSpeed, baseWallSlide);
        left += Stat("Loot Range", s.lootRange, baseLootRange);
        left += Stat("Luck", s.luck, baseLuck);
        left += "\n";

        // -- Combat --
        left += "<b><color=#FFD700>== COMBAT ==</color></b>\n";
        left += $"{"HP",-18}<color=#FFFFFF>{(hp != null ? hp.CurrentHP : 0)} / {s.maxHP}</color>\n";
        left += Stat("Arrow Damage", s.arrowDamage, baseArrowDamage);
        left += Stat("Dash Damage", s.dashDamage, baseDashDamage);
        left += StatPct("Crit Chance", s.critChance, baseCritChance);
        left += StatPct("Crit Multi", s.critMultiplier, baseCritMultiplier);
        left += Stat("Knockback", s.knockbackForce, baseKnockback);
        left += StatPct("Lifesteal", s.lifeSteal, baseLifesteal);
        left += StatPct("Burn", s.burnStrength, baseBurn);
        left += StatPct("Freeze", s.freezeStrength, baseFreeze);
        left += StatPct("Holy", s.holyStrength, baseHoly);
        left += StatPct("Shock", s.shockStrength, baseShock);
        left += "\n";

        // -- Ammo --
        left += "<b><color=#FFD700>== AMMO ==</color></b>\n";
        left += StatInt("Arrows", s.CurrentArrows, s.CurrentArrows);
        left += $"{"Max Arrows",-18}";
        left += StatInt("", s.MaxArrows, baseMaxArrows).TrimStart();

        return left;
    }

    // ================================================================
    //  RIGHT COLUMN — Active Upgrades + Run History
    // ================================================================

    private string BuildRight()
    {
        var s = playerStats;

        // -- Active Upgrades (hoverable via TMP links) --
        string right = "<b><color=#FF99FF>== ACTIVE UPGRADES ==</color></b>\n";
        if (upgradeManager != null)
        {
            bool any = false;
            foreach (var id in upgradeManager.GetActiveUpgradeIds())
            {
                PlayerUpgrade upgrade = upgradeManager.GetUpgrade(id);
                string name = upgrade != null ? upgrade.displayName : id;
                right += $"  <link=\"{id}\"><color=#AAFFAA>{name}</color></link>\n";
                any = true;
            }
            if (!any) right += "  <color=#888888>None</color>\n";
        }
        right += "\n";

        // -- Run History --
        right +=
            "<b><color=#FF6666>== HISTORY: MOVEMENT ==</color></b>\n" +
            $"{"Distance",-18}{s.totalDistance:F0}m\n" +
            $"{"Jumps",-18}{s.jumpsPerformed}\n" +
            $"{"Wall Slides",-18}{s.wallSlideCount} ({s.totalWallSlideDuration:F1}s)\n" +
            $"{"Hovers",-18}{s.hoverCount} ({s.totalHoverDuration:F1}s)\n" +
            $"{"Dashes",-18}{s.dashCount} (E:{s.dashesHitEnemy} W:{s.dashesHitWall})\n" +
            "\n" +
            "<b><color=#FF6666>== HISTORY: COMBAT ==</color></b>\n" +
            $"{"Arrows Fired",-18}{s.totalArrowsFired} (L:{s.weakArrowsFired} M:{s.mediumArrowsFired} S:{s.chargedArrowsFired})\n" +
            $"{"Arrows Hit",-18}{s.arrowsHitEnemy}\n" +
            $"{"Kills",-18}{s.enemiesKilled}\n" +
            $"{"Dmg Dealt",-18}{s.totalDamageDealt:F0}\n" +
            $"{"Crits",-18}{s.critsLanded}\n" +
            $"{"Status",-18}B:{s.burnApplied} F:{s.freezeApplied} H:{s.holyApplied} S:{s.shockApplied}\n" +
            "\n" +
            "<b><color=#FF6666>== HISTORY: RESOURCES ==</color></b>\n" +
            $"{"Souls",-18}{s.soulsCollected}\n" +
            $"{"XP",-18}{s.xpGained:F0}\n" +
            $"{"Energy",-18}{s.energyGainedTotal:F0}\n" +
            $"{"  Kills",-18}{s.energyFromKills:F0}\n" +
            $"{"  Falling",-18}{s.energyFromFalling:F0}\n" +
            $"{"  Lava",-18}{s.energyFromLava:F0}\n" +
            $"{"  Wall Slide",-18}{s.energyFromWallSlide:F0}\n" +
            "\n" +
            "<b><color=#FF6666>== HISTORY: SURVIVAL ==</color></b>\n" +
            $"{"Dmg Taken",-18}{s.damageTaken:F0}\n" +
            $"{"Times Hit",-18}{s.timesHit}\n" +
            $"{"HP Restored",-18}{s.hpRestored:F0}\n" +
            $"{"Extra Life",-18}{(PlayerHealth.HasExtraLife ? "<color=#66FF66>YES</color>" : "<color=#888888>no</color>")}\n" +
            $"{"Layers Done",-18}{s.layersCompleted}\n";

        return right;
    }
}
